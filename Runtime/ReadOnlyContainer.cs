using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Kryz.DI
{
	public class ReadOnlyContainer : IResolver, IDisposable
	{
		public readonly ReadOnlyContainer? Parent;
		public readonly IInjector Injector;

		private readonly Dictionary<Type, object> objects;
		private readonly IReadOnlyDictionary<Type, Registration> registrations;

		internal ReadOnlyContainer(IInjector injector, IReadOnlyDictionary<Type, Registration> registrations, Dictionary<Type, object> objects)
		{
			Injector = injector;
			this.objects = objects;
			this.registrations = registrations;
		}

		internal ReadOnlyContainer(ReadOnlyContainer parent, IReadOnlyDictionary<Type, Registration> registrations, Dictionary<Type, object> objects)
		{
			Parent = parent;
			Injector = parent.Injector;
			this.objects = objects;
			this.registrations = registrations;
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

			for (ReadOnlyContainer? container = this; container != null; container = container.Parent)
			{
				if (container.registrations.TryGetValue(type, out Registration registration))
				{
					obj = registration.Lifetime switch
					{
						Lifetime.Singleton => container.GetOrCreateObject(type, registration.Type),
						Lifetime.Scoped => GetOrCreateObject(type, registration.Type),
						Lifetime.Transient => CreateAndInjectObject(registration.Type),
						_ => throw new NotImplementedException(),
					};
					return true;
				}
			}

			obj = default;
			return false;
		}

		public bool TryGetType<T>([MaybeNullWhen(returnValue: false)] out Type type)
		{
			return TryGetType(typeof(T), out type);
		}

		public bool TryGetType(Type type, [MaybeNullWhen(returnValue: false)] out Type resolvedType)
		{
			for (ReadOnlyContainer? container = this; container != null; container = container.Parent)
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

		public void Dispose()
		{
			foreach (object item in objects.Values)
			{
				if (item is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
		}

		~ReadOnlyContainer()
		{
			Dispose();
		}

		private object GetOrCreateObject(Type type, Type resolvedType)
		{
			if (!objects.TryGetValue(type, out object obj))
			{
				obj = CreateAndInjectObject(resolvedType);
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