using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Kryz.Collections;
using Kryz.DI.Exceptions;

namespace Kryz.DI.Internal
{
	internal class Container : IContainer
	{
		private readonly Dictionary<Type, object> objects;
		private readonly Dictionary<Type, Registration> registrations;
		private readonly PooledList<Container> childScopes;

		public readonly IInjector Injector;
		public readonly Container? Parent;
		public readonly IReadOnlyList<Container> ChildScopes;

		IInjector IContainer.Injector => Injector;
		IContainer? IContainer.Parent => Parent;
		IReadOnlyList<IContainer> IContainer.ChildScopes => ChildScopes;

		internal Container(IInjector injector, Dictionary<Type, Registration> registrations, Dictionary<Type, object> objects)
		{
			Injector = injector;
			this.objects = objects;
			this.registrations = registrations;
			ChildScopes = childScopes = new PooledList<Container>();
		}

		internal Container(Container parent, Dictionary<Type, Registration> registrations, Dictionary<Type, object> objects) : this(parent.Injector, registrations, objects)
		{
			Parent = parent;
			Parent.childScopes.Add(this);
		}

		public T ResolveObject<T>()
		{
			return (T)ResolveObject(typeof(T));
		}

		public object ResolveObject(Type type)
		{
			if (TryResolveObject(type, out object? obj))
			{
				return obj;
			}
			throw new InjectionException($"Type {type.FullName} has not been registered.");
		}

		public Type ResolveType<T>()
		{
			return ResolveType(typeof(T));
		}

		public Type ResolveType(Type type)
		{
			if (TryResolveType(type, out Type? resolvedType))
			{
				return resolvedType;
			}
			throw new InjectionException($"Type {type.FullName} has not been registered.");
		}

		public bool TryResolveObject<T>([MaybeNullWhen(returnValue: false)] out T obj)
		{
			if (TryResolveObject(typeof(T), out object? o))
			{
				obj = (T)o;
				return true;
			}
			obj = default;
			return false;
		}

		public bool TryResolveObject(Type type, [MaybeNullWhen(returnValue: false)] out object obj)
		{
			if (objects.TryGetValue(type, out obj))
			{
				return true;
			}

			for (Container? container = this; container != null; container = container.Parent)
			{
				if (container.registrations.TryGetValue(type, out Registration registration))
				{
					obj = registration.Lifetime switch
					{
						Lifetime.Singleton => container.GetOrCreateObject(type, registration.Type),
						Lifetime.Scoped => GetOrCreateObject(type, registration.Type),
						Lifetime.Transient => CreateAndInjectObject(registration.Type),
						_ => throw new NotImplementedException($"No implementation for {nameof(Lifetime)}.{registration.Lifetime}"),
					};
					return true;
				}
			}

			obj = default;
			return false;
		}

		public bool TryResolveType<T>([MaybeNullWhen(returnValue: false)] out Type resolvedType)
		{
			return TryResolveType(typeof(T), out resolvedType);
		}

		public bool TryResolveType(Type type, [MaybeNullWhen(returnValue: false)] out Type resolvedType)
		{
			for (Container? container = this; container != null; container = container.Parent)
			{
				if (container.registrations.TryGetValue(type, out Registration registration))
				{
					resolvedType = registration.Type;
					return true;
				}
			}
			resolvedType = default;
			return false;
		}

		public IBuilder CreateScopeBuilder()
		{
			return new Builder(this);
		}

		public IContainer CreateScope()
		{
			return new Builder(this).Build();
		}

		public IContainer CreateScope(Action<IScopeBuilder> builderAction)
		{
			Builder builder = new(this);
			builderAction(builder);
			return builder.Build();
		}

		public void Inject(object obj)
		{
			Injector.Inject(obj, this);
		}

		public void Dispose()
		{
			Parent?.childScopes.Remove(this);

			for (int i = childScopes.Count - 1; i >= 0; i--)
			{
				childScopes[i].Dispose();
			}

			foreach (object obj in objects.Values)
			{
				if (obj != this && obj is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}

			objects.Clear();
			registrations.Clear();
			childScopes.Clear();
		}

		~Container()
		{
			Dispose();
		}

		private object GetOrCreateObject(Type type, Type resolvedType)
		{
			if (!objects.TryGetValue(type, out object obj))
			{
				objects[type] = obj = CreateAndInjectObject(resolvedType);
			}
			return obj;
		}

		private object CreateAndInjectObject(Type resolvedType)
		{
			object obj = Injector.CreateObject(resolvedType, this);
			Injector.Inject(obj, this);
			return obj;
		}
	}
}