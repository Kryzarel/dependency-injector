using System;
using System.Collections.Generic;

namespace Kryz.DI
{
	internal readonly struct DependencyGraph<T> where T : IEnumerable<Type>
	{
		private readonly IResolver resolver;
		private readonly IInjector injector;

		public readonly IReadOnlyDictionary<Type, IReadOnlyList<Type>>? MissingDependencies;
		public readonly IReadOnlyDictionary<Type, IReadOnlyList<Type>>? CircularDependencies;

		internal DependencyGraph(IResolver resolver, IInjector injector, T types)
		{
			this.resolver = resolver;
			this.injector = injector;

			MissingDependencies = null;
			CircularDependencies = null;

			List<Type>? visitedTypes = null;
			Dictionary<Type, IReadOnlyList<Type>>? missingDependencies = null;
			Dictionary<Type, IReadOnlyList<Type>>? circularDependencies = null;

			foreach (Type type in types)
			{
				if (HasMissingDependency(type, out IReadOnlyList<Type> missing))
				{
					missingDependencies ??= new Dictionary<Type, IReadOnlyList<Type>>();
					missingDependencies[type] = missing;
				}

				visitedTypes ??= new();
				if (HasCircularDependency(type, visitedTypes))
				{
					circularDependencies ??= new Dictionary<Type, IReadOnlyList<Type>>();
					circularDependencies[type] = visitedTypes.ToArray();
				}
				visitedTypes.Clear();
			}

			MissingDependencies = missingDependencies;
			CircularDependencies = circularDependencies;
		}

		private bool HasMissingDependency(Type type, out IReadOnlyList<Type> missing)
		{
			List<Type>? missingTypes = null;

			IReadOnlyList<Type> dependencies = injector.GetDependencies(type);
			for (int i = 0; i < dependencies.Count; i++)
			{
				Type dependency = dependencies[i];
				if (!resolver.TryGetType(type, out _))
				{
					missingTypes ??= new List<Type>();
					missingTypes.Add(dependency);
				}
			}

			missing = missingTypes != null ? missingTypes : Array.Empty<Type>();
			return missing.Count > 0;
		}

		private bool HasCircularDependency(Type type, List<Type> visitedTypes)
		{
			if (visitedTypes.Contains(type))
			{
				visitedTypes.Add(type);
				return true;
			}
			visitedTypes.Add(type);

			IReadOnlyList<Type> dependencies = injector.GetDependencies(type);
			for (int i = 0; i < dependencies.Count; i++)
			{
				Type dependency = dependencies[i];
				if (resolver.TryGetType(dependency, out Type? resolvedType))
				{
					if (HasCircularDependency(resolvedType!, visitedTypes))
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}