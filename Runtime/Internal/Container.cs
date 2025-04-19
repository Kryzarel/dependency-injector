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
		private readonly IReadOnlyDictionary<Type, Registration> registrations;
		private readonly PooledList<Container> childScopes;

		public readonly IInjector Injector;
		public readonly Container? Parent;
		public readonly IReadOnlyList<Container> ChildScopes;

		IInjector IContainer.Injector => Injector;
		IContainer? IContainer.Parent => Parent;
		IReadOnlyList<IContainer> IContainer.ChildScopes => ChildScopes;

		internal Container(IInjector injector, IReadOnlyDictionary<Type, Registration> registrations, Dictionary<Type, object> objects)
		{
			Injector = injector;
			this.objects = objects;
			this.registrations = registrations;
			ChildScopes = childScopes = new PooledList<Container>();
		}

		internal Container(Container parent, IReadOnlyDictionary<Type, Registration> registrations, Dictionary<Type, object> objects) : this(parent.Injector, registrations, objects)
		{
			Parent = parent;
		}

		public T GetObject<T>()
		{
			return (T)GetObject(typeof(T));
		}

		public object GetObject(Type type)
		{
			if (TryGetObject(type, out object? obj))
			{
				return obj;
			}
			throw new InjectionException($"Type {type.FullName} has not been registered.");
		}

		public Type GetType<T>()
		{
			return GetType(typeof(T));
		}

		public Type GetType(Type type)
		{
			if (TryGetType(type, out Type? resolvedType))
			{
				return resolvedType;
			}
			throw new InjectionException($"Type {type.FullName} has not been registered.");
		}

		public bool TryGetObject<T>([MaybeNullWhen(returnValue: false)] out T obj)
		{
			if (TryGetObject(typeof(T), out object? o))
			{
				obj = (T)o;
				return true;
			}
			obj = default;
			return false;
		}

		public bool TryGetObject(Type type, [MaybeNullWhen(returnValue: false)] out object obj)
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

		public bool TryGetType<T>([MaybeNullWhen(returnValue: false)] out Type resolvedType)
		{
			return TryGetType(typeof(T), out resolvedType);
		}

		public bool TryGetType(Type type, [MaybeNullWhen(returnValue: false)] out Type resolvedType)
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

		public IContainer CreateScope()
		{
			Container container = new Builder(this).Build_Internal();
			childScopes.Add(container);
			return container;
		}

		public IContainer CreateScope(Action<Builder> build)
		{
			Builder builder = new(this);
			build(builder);
			Container container = builder.Build_Internal();
			childScopes.Add(container);
			return container;
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
			childScopes.Clear();

			foreach (object item in objects.Values)
			{
				if (item != this && item is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
			objects.Clear();
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