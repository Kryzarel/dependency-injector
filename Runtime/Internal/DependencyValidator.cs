using System;
using System.Collections.Generic;
using Kryz.Collections;
using Kryz.Utils;

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
			NonAllocList<Type> visitedTypes = new();
			Dictionary<Type, IReadOnlyList<Type>>? missing = null;
			Dictionary<Type, IReadOnlyList<Type>>? circular = null;

			foreach (KeyValuePair<Type, Registration> item in registrations)
			{
				Type type = item.Key;

				if (HasMissingDependency(type, resolver, injector, out IReadOnlyList<Type> missingTypes))
				{
					missing ??= new Dictionary<Type, IReadOnlyList<Type>>();
					missing[type] = missingTypes;
				}

				if (HasCircularDependency(type, injector, registrations, objects, ref visitedTypes))
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
				if (!resolver.TryGetType(dependency, out _))
				{
					missingTypes ??= new List<Type>();
					missingTypes.Add(dependency);
				}
			}

			missing = missingTypes != null ? missingTypes : Array.Empty<Type>();
			return missing.Count > 0;
		}

		public static bool HasCircularDependency<TList>(Type type, IInjector injector, IReadOnlyDictionary<Type, Registration> registrations, IReadOnlyDictionary<Type, object> objects, ref TList visitedTypes) where TList : IList<Type>
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
					if (HasCircularDependency(registration.Type, injector, registrations, objects, ref visitedTypes))
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