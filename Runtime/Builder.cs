using System;
using System.Collections.Generic;
using System.Linq;
using Kryz.DI.Reflection;

namespace Kryz.DI
{
	public class Builder
	{
		private readonly ReadOnlyContainer? parent;
		private readonly Dictionary<Type, object> objects = new();
		private readonly Dictionary<Type, Registration> registrations = new();

		public Builder(ReadOnlyContainer? parent = null)
		{
			this.parent = parent;
		}

		public Builder Register<T>(Lifetime lifetime)
		{
			return Register<T, T>(lifetime);
		}

		public Builder RegisterInstance<T>(T obj) where T : notnull
		{
			return RegisterInstance<T, T>(obj);
		}

		public Builder Register<TBase, TDerived>(Lifetime lifetime) where TDerived : TBase
		{
			registrations[typeof(TBase)] = new Registration(typeof(TDerived), lifetime);
			return this;
		}

		public Builder RegisterInstance<TBase, TDerived>(TDerived obj) where TDerived : notnull, TBase
		{
			objects[typeof(TBase)] = obj;
			registrations[typeof(TBase)] = new Registration(typeof(TDerived), Lifetime.Singleton);
			return this;
		}

		public ReadOnlyContainer Build()
		{
			// Create copies of the dictionaries to avoid modifications to the Container if the Builder keeps being registered to
			Dictionary<Type, object> obj = new(objects);
			Dictionary<Type, Registration> reg = new(registrations);
			IInjector injector = parent?.Injector ?? new ReflectionInjector();

			ReadOnlyContainer container = parent != null ? new ReadOnlyContainer(parent, reg, obj) : new ReadOnlyContainer(injector, reg, obj);

			// Make one last modification to the dictionaries: register the Container itself as IResolver
			obj[typeof(IResolver)] = container;
			reg[typeof(IResolver)] = new Registration(typeof(IResolver), Lifetime.Singleton);

			DependencyGraph<Dictionary<Type, Registration>.KeyCollection> graph = new(container, injector, reg.Keys);
			if (graph.MissingDependencies != null && graph.MissingDependencies.Count > 0)
			{
				throw new MissingDependencyException($"Cannot build a container with missing dependencies: {FormatMissingDependencies(graph.MissingDependencies)}");
			}
			if (graph.CircularDependencies != null && graph.CircularDependencies.Count > 0)
			{
				throw new CircularDependencyException($"Cannot build a container with circular dependencies: {FormatCircularDependencies(graph.CircularDependencies)}");
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