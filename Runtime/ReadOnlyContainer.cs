using System;
using System.Collections.Generic;

namespace Kryz.DI
{
	public class ReadOnlyContainer : IResolver
	{
		public readonly ReadOnlyContainer? Parent;

		private readonly IInjector injector;
		private readonly IReadOnlyDictionary<Type, Registration> registrations;

		internal ReadOnlyContainer(IInjector injector, IReadOnlyDictionary<Type, Registration> registrations)
		{
			this.injector = injector;
			this.registrations = registrations;
		}

		internal ReadOnlyContainer(ReadOnlyContainer parent, IInjector injector, IReadOnlyDictionary<Type, Registration> registrations)
		{
			Parent = parent;
			this.injector = injector;
			this.registrations = registrations;
		}

		public T GetObject<T>()
		{
			throw new NotImplementedException();
		}

		public object GetObject(Type type)
		{
			throw new NotImplementedException();
		}

		public Type GetType<T>()
		{
			throw new NotImplementedException();
		}

		public Type GetType(Type type)
		{
			throw new NotImplementedException();
		}

		public bool TryGetObject<T>(out T? obj)
		{
			if (TryGetObject(typeof(T), out object? o))
			{
				obj = (T)o!;
				return true;
			}

			obj = default;
			return false;
		}

		public bool TryGetObject(Type type, out object? obj)
		{
			for (ReadOnlyContainer? container = this; container != null; container = container.Parent)
			{
				if (container.registrations.TryGetValue(type, out Registration registration))
				{
					obj = registration.Lifetime switch
					{
						Lifetime.Singleton => registration.Object,
						Lifetime.Scoped => registration.Object,
						Lifetime.Transient => injector.CreateObject(type, this),
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
			throw new NotImplementedException();
		}

		public bool TryGetType(Type type, out Type? resolvedType)
		{
			throw new NotImplementedException();
		}
	}
}