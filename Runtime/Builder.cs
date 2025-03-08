using System;
using System.Collections.Generic;
using Kryz.DI.Reflection;

namespace Kryz.DI
{
	public class Builder
	{
		private readonly IInjector injector;
		private readonly Dictionary<Type, Registration> registrations = new();

		public Builder(IInjector? injector = null)
		{
			this.injector = injector ?? new ReflectionInjector();
		}

		public Builder Register<T>(Lifetime lifetime) => Register<T, T>(lifetime);
		public Builder RegisterInstance<T>(T obj) where T : notnull => RegisterInstance<T, T>(obj);

		public Builder Register<TBase, TDerived>(Lifetime lifetime) where TDerived : TBase
		{
			registrations[typeof(TBase)] = new Registration(typeof(TDerived), null, lifetime);
			return this;
		}

		public Builder RegisterInstance<TBase, TDerived>(TDerived obj) where TDerived : notnull, TBase
		{
			registrations[typeof(TBase)] = new Registration(typeof(TDerived), obj, Lifetime.Singleton);
			return this;
		}

		public ReadOnlyContainer Build()
		{
			foreach (var item in registrations)
			{

			}
			return new ReadOnlyContainer(injector, registrations);
		}
	}
}