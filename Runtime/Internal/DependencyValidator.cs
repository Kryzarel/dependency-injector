using System;
using System.Collections.Generic;
using Kryz.Collections;

namespace Kryz.DI.Internal
{
	internal static class DependencyValidator
	{
		public readonly struct Data
		{
			public readonly IReadOnlyDictionary<Type, IReadOnlyList<Type>>? MissingDependencies;
			public readonly IReadOnlyDictionary<Type, IReadOnlyList<Type>>? CircularDependencies;

			internal Data(IReadOnlyDictionary<Type, IReadOnlyList<Type>>? missing, IReadOnlyDictionary<Type, IReadOnlyList<Type>>? circular)
			{
				MissingDependencies = missing;
				CircularDependencies = circular;
			}
		}

		public static Data Validate<T>(ITypeResolver resolver, IInjector injector, T registrations, IReadOnlyDictionary<Type, object> objects) where T : IReadOnlyDictionary<Type, Registration>
		{
			using PooledList<Type> visitedTypes = PooledList<Type>.Rent();
			Dictionary<Type, IReadOnlyList<Type>>? missing = null;
			Dictionary<Type, IReadOnlyList<Type>>? circular = null;

			foreach (KeyValuePair<Type, Registration> item in registrations)
			{
				Type resolvedType = item.Value.Type;

				if (HasMissingDependency(resolvedType, resolver, injector, out IReadOnlyList<Type> missingTypes))
				{
					missing ??= new Dictionary<Type, IReadOnlyList<Type>>();
					missing[item.Key] = missingTypes;
				}

				if (HasCircularDependency(resolvedType, injector, registrations, objects, visitedTypes))
				{
					circular ??= new Dictionary<Type, IReadOnlyList<Type>>();
					circular[item.Key] = visitedTypes.ToArray();
				}
				visitedTypes.Clear();
			}
			visitedTypes.Dispose();

			return new Data(missing, circular);
		}

		public static bool HasMissingDependency(Type type, ITypeResolver resolver, IInjector injector, out IReadOnlyList<Type> missing)
		{
			List<Type>? missingTypes = null;

			IReadOnlyList<Type> dependencies = injector.GetDependencies(type);
			for (int i = 0; i < dependencies.Count; i++)
			{
				Type dependency = dependencies[i];
				if (!resolver.TryResolveType(dependency, out _))
				{
					missingTypes ??= new List<Type>();
					missingTypes.Add(dependency);
				}
			}

			missing = missingTypes != null ? missingTypes : Array.Empty<Type>();
			return missing.Count > 0;
		}

		public static bool HasCircularDependency(Type type, IInjector injector, IReadOnlyDictionary<Type, Registration> registrations, IReadOnlyDictionary<Type, object> objects, IList<Type> visitedTypes)
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
				if (registrations.TryGetValue(dependency, out Registration registration) && !objects.ContainsKey(dependency))
				{
					if (HasCircularDependency(registration.Type, injector, registrations, objects, visitedTypes))
					{
						return true;
					}
				}
			}

			visitedTypes.RemoveAt(visitedTypes.Count - 1);
			return false;
		}
	}
}