using System;
using System.Collections.Generic;
using System.Linq;
using Kryz.DI.Exceptions;
using Kryz.DI.Internal;
using Kryz.DI.Reflection;

namespace Kryz.DI
{
	public class Builder : IBuilder
	{
		private static readonly ReflectionInjector reflectionInjector;
		private static readonly ExpressionInjector expressionInjector;

		private readonly Container? parent;
		private readonly IInjector injector;
		private readonly Dictionary<Type, object> objects = new();
		private readonly Dictionary<Type, Registration> registrations = new();

		static Builder()
		{
			ReflectionCache reflectionCache = new();
			reflectionInjector = new(reflectionCache);
			expressionInjector = new(reflectionCache);
		}

		internal Builder(Container parent)
		{
			this.parent = parent;
			injector = parent.Injector;
		}

		public Builder(bool useExpressionInjector = false)
		{
			injector = useExpressionInjector ? expressionInjector : reflectionInjector;
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
			Register(obj, objects, registrations);
			return this;
		}

		IBuilder IBuilder.Register<T>(Lifetime lifetime) => Register<T>(lifetime);
		IBuilder IBuilder.Register<TBase, TDerived>(Lifetime lifetime) => Register<TBase, TDerived>(lifetime);
		IBuilder IBuilder.Register<T>(T obj) => Register(obj);

		public IContainer Build()
		{
			Dictionary<Type, object> objects = new(this.objects);
			Dictionary<Type, Registration> registrations = new(this.registrations);

			Container container = parent != null ? new Container(parent, objects, registrations) : new Container(injector, objects, registrations);

			// Register the Container itself
			Register<IContainer>(container, objects, registrations);
			Register<IObjectResolver>(container, objects, registrations);
			Register<ITypeResolver>(container, objects, registrations);

			DependencyValidator.Data data = DependencyValidator.Validate(container, container.Injector, objects, registrations);
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

		private static void Register<T>(T obj, Dictionary<Type, object> objects, Dictionary<Type, Registration> registrations) where T : notnull
		{
			objects[typeof(T)] = obj;
			registrations[typeof(T)] = new Registration(obj.GetType(), Lifetime.Singleton);
		}

		private static string FormatMissingDependencies(IReadOnlyDictionary<Type, IReadOnlyList<Type>> dict)
		{
			return string.Join('\n', dict.Select(item => $"{{ {item.Key} - Missing: {string.Join(", ", item.Value)} }}"));
		}

		private static string FormatCircularDependencies(IReadOnlyDictionary<Type, IReadOnlyList<Type>> dict)
		{
			return string.Join('\n', dict.Select(item => $"{{ {item.Key} - Path: {string.Join(" -> ", item.Value)} }}"));
		}
	}
}