using System;
using System.Collections.Generic;

namespace Kryz.DI
{
	public class ReadOnlyContainer : IResolver
	{
		public readonly ReadOnlyContainer? Parent;

		private readonly IInjector injector;
		private readonly Dictionary<Type, object> objects;
		private readonly IReadOnlyDictionary<Type, Registration> registrations;

		internal ReadOnlyContainer(IInjector injector, IReadOnlyDictionary<Type, Registration> registrations, Dictionary<Type, object> objects)
		{
			this.injector = injector;
			this.objects = objects;
			this.registrations = registrations;
		}

		internal ReadOnlyContainer(ReadOnlyContainer parent, IReadOnlyDictionary<Type, Registration> registrations, Dictionary<Type, object> objects)
		{
			Parent = parent;
			injector = parent.injector;
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
				return obj!;
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
				return resolvedType!;
			}
			throw new InjectionException($"Type {type.FullName} has not been registered.");
		}

		public bool TryGetObject<T>(out T? obj)
		{
			bool result = TryGetObject(typeof(T), out object? o);
			obj = result ? (T)o! : default;
			return result;
		}

		public bool TryGetObject(Type type, out object? obj)
		{
			for (ReadOnlyContainer? container = this; container != null; container = container.Parent)
			{
				if (container.registrations.TryGetValue(type, out Registration registration))
				{
					obj = registration.Lifetime switch
					{
						Lifetime.Singleton => container.GetOrCreateObject(type),
						Lifetime.Scoped => GetOrCreateObject(type),
						Lifetime.Transient => CreateAndInjectObject(type),
						_ => throw new NotImplementedException(),
					};
					return true;
				}
			}
			obj = default;
			return false;
		}

		public bool TryGetType<T>(out Type? type)
		{
			return TryGetType(typeof(T), out type);
		}

		public bool TryGetType(Type type, out Type? resolvedType)
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

		private object GetOrCreateObject(Type type)
		{
			if (!objects.TryGetValue(type, out object obj))
			{
				obj = CreateAndInjectObject(type);
			}
			return obj;
		}

		private object CreateAndInjectObject(Type type)
		{
			object obj = injector.CreateObject(type, this);
			injector.Inject(type, this);
			return obj;
		}
	}
}