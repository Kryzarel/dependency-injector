using System;
using System.Collections.Generic;
using System.Linq;
using Kryz.DI.Exceptions;
using Kryz.DI.Reflection;

namespace Kryz.DI
{
	public ref struct Builder
	{
		private readonly ReadOnlyContainer? parent;
		private readonly Dictionary<Type, object> objects;
		private readonly Dictionary<Type, Registration> registrations;
		private bool hasBuilt;

		public Builder(ReadOnlyContainer? parent = null)
		{
			this.parent = parent;
			objects = new Dictionary<Type, object>();
			registrations = new Dictionary<Type, Registration>();
			hasBuilt = false;
		}

		public readonly Builder Register<T>(Lifetime lifetime)
		{
			return Register<T, T>(lifetime);
		}

		public readonly Builder Register<TBase, TDerived>(Lifetime lifetime) where TDerived : TBase
		{
			registrations[typeof(TBase)] = new Registration(typeof(TDerived), lifetime);
			return this;
		}

		public readonly Builder RegisterInstance<T>(T obj) where T : notnull
		{
			objects[typeof(T)] = obj;
			registrations[typeof(T)] = new Registration(obj.GetType(), Lifetime.Singleton);
			return this;
		}

		public IContainer Build()
		{
			if (hasBuilt)
			{
				throw new InvalidOperationException($"Can't build from the same {nameof(Builder)} more than once.");
			}

			hasBuilt = true;
			IInjector injector = parent?.Injector ?? new ReflectionInjector();
			ReadOnlyContainer container = parent != null ? new ReadOnlyContainer(parent, registrations, objects) : new ReadOnlyContainer(injector, registrations, objects);

			// Register the Container itself
			RegisterInstance<IContainer>(container);
			RegisterInstance<IResolver>(container);
			RegisterInstance<IObjectResolver>(container);
			RegisterInstance<ITypeResolver>(container);

			DependencyValidator.Data data = DependencyValidator.Validate(container, injector, registrations.Keys);
			if (data.MissingDependencies != null && data.MissingDependencies.Count > 0)
			{
				throw new MissingDependencyException($"Can't build a container with missing dependencies: {FormatMissingDependencies(data.MissingDependencies)}");
			}
			if (data.CircularDependencies != null && data.CircularDependencies.Count > 0)
			{
				throw new CircularDependencyException($"Can't build a container with circular dependencies: {FormatCircularDependencies(data.CircularDependencies)}");
			}
			return container;
		}

		private static string FormatMissingDependencies(IReadOnlyDictionary<Type, IReadOnlyList<Type>> dict)
		{
			return string.Join('\n', dict.Select(item => $"{{ {item.Key} - Missing: {string.Join(", ", item.Value)} }}"));
		}

		private static string FormatCircularDependencies(IReadOnlyDictionary<Type, IReadOnlyList<Type>> dict)
		{
			return string.Join('\n', dict.Select(item => $"{{ {item.Key} - Path: {string.Join("->", item.Value)} }}"));
		}
	}
}