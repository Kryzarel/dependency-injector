using System;
using System.Collections.Generic;
using System.Linq;
using Kryz.DI.Exceptions;
using Kryz.DI.Reflection;

namespace Kryz.DI
{
	public class Builder
	{
		private readonly Container? parent;
		private readonly Dictionary<Type, object> objects = new();
		private readonly Dictionary<Type, Registration> registrations = new();

		private Container? container;

		internal Builder(Container parent)
		{
			this.parent = parent;
		}

		public Builder()
		{
		}

		public Builder Register<T>(Lifetime lifetime)
		{
			return Register<T, T>(lifetime);
		}

		public Builder Register<TBase, TDerived>(Lifetime lifetime) where TDerived : TBase
		{
			registrations[typeof(TBase)] = new Registration(typeof(TDerived), lifetime);
			return this;
		}

		public Builder Register<T>(T obj) where T : notnull
		{
			objects[typeof(T)] = obj;
			registrations[typeof(T)] = new Registration(obj.GetType(), Lifetime.Singleton);
			return this;
		}

		public IContainer Build()
		{
			return Build_Internal();
		}

		internal Container Build_Internal()
		{
			if (container != null)
			{
				throw new InvalidOperationException($"Can't build from the same {nameof(Builder)} more than once.");
			}

			IInjector injector = parent?.Injector ?? new ReflectionInjector();
			container = parent != null ? new Container(parent, registrations, objects) : new Container(injector, registrations, objects);

			// Register the Container itself
			Register<IContainer>(container);
			Register<IObjectResolver>(container);
			Register<ITypeResolver>(container);

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