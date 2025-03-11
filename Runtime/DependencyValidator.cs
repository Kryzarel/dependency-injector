using System;
using System.Collections.Generic;
using Kryz.Collections;
using Kryz.Utils;

namespace Kryz.DI
{
	public static class DependencyValidator
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

		public static Data Validate<T>(ITypeResolver resolver, IInjector injector, T types) where T : IEnumerable<Type>
		{
			NonAllocList<Type> visitedTypes = new();
			Dictionary<Type, IReadOnlyList<Type>>? missing = null;
			Dictionary<Type, IReadOnlyList<Type>>? circular = null;

			foreach (Type type in types)
			{
				if (HasMissingDependency(type, resolver, injector, out IReadOnlyList<Type> missingTypes))
				{
					missing ??= new Dictionary<Type, IReadOnlyList<Type>>();
					missing[type] = missingTypes;
				}

				if (HasCircularDependency(type, resolver, injector, ref visitedTypes))
				{
					circular ??= new Dictionary<Type, IReadOnlyList<Type>>();
					circular[type] = visitedTypes.ToArray<Type, NonAllocList<Type>>();
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
				if (!resolver.TryGetType(type, out _))
				{
					missingTypes ??= new List<Type>();
					missingTypes.Add(dependency);
				}
			}

			missing = missingTypes != null ? missingTypes : Array.Empty<Type>();
			return missing.Count > 0;
		}

		public static bool HasCircularDependency<TList>(Type type, ITypeResolver resolver, IInjector injector, ref TList visitedTypes) where TList : IList<Type>
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
					if (HasCircularDependency(resolvedType, resolver, injector, ref visitedTypes))
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}