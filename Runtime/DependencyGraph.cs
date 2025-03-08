using System;
using System.Collections.Generic;
using Kryz.DI.Reflection;

namespace Kryz.DI
{
	internal class DependencyGraph<T1, T2> where T1 : IReadOnlyDictionary<Type, Registration> where T2 : IReadOnlyDictionary<Type, object>
	{
		private readonly ReflectionCache reflectionCache = ReflectionCache.Instance;
		private readonly ReadOnlyContainer? parent;
		private readonly IReadOnlyDictionary<Type, object> objects;
		private readonly IReadOnlyDictionary<Type, Registration> registrations;

		public readonly IReadOnlyDictionary<Type, IReadOnlyList<Type>>? MissingDependencies;
		public readonly IReadOnlyDictionary<Type, IReadOnlyList<Type>>? CircularDependencies;

		internal static DependencyGraph<T1, T2> Create(ReadOnlyContainer? parent, T1 registrations, T2 objects)
		{
			return new DependencyGraph<T1, T2>(parent, registrations, objects);
		}

		internal DependencyGraph(ReadOnlyContainer? parent, T1 registrations, T2 objects)
		{
			this.parent = parent;
			this.objects = objects;
			this.registrations = registrations;

			Dictionary<Type, IReadOnlyList<Type>>? missingDependencies = null;
			Dictionary<Type, IReadOnlyList<Type>>? circularDependencies = null;
			List<Type>? visitedTypes = null;

			foreach (Type type in registrations.Keys)
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
			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);

			foreach (Type dependency in new DependenciesEnumerator(info))
			{
				if (!TryGetType(dependency, out _))
				{
					missingTypes ??= new List<Type>();
					missingTypes.Add(dependency);
				}
			}

			missing = missingTypes != null ? missingTypes : Array.Empty<Type>();
			return missing.Count > 0;
		}

		private bool TryGetType(Type type, out Type? registeredType)
		{
			if (registrations.TryGetValue(type, out Registration registration))
			{
				registeredType = registration.Type;
				return true;
			}

			for (ReadOnlyContainer? container = parent; container != null; container = container.Parent)
			{
				if (container.TryGetType(type, out registeredType))
				{
					return true;
				}
			}

			registeredType = default;
			return false;
		}

		private bool HasCircularDependency(Type type, List<Type> visitedTypes)
		{
			if (visitedTypes.Contains(type))
			{
				visitedTypes.Add(type);
				return true;
			}
			visitedTypes.Add(type);

			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);

			foreach (Type dependency in new DependenciesEnumerator(info))
			{
				if (registrations.TryGetValue(dependency, out Registration registration) && !objects.ContainsKey(type))
				{
					if (HasCircularDependency(registration.Type, visitedTypes))
					{
						return true;
					}
				}
			}
			return false;
		}

		private struct DependenciesEnumerator
		{
			private readonly ReflectionCache.InjectionInfo info;

			private Type current;
			private int constructorParamIndex;
			private int fieldIndex;
			private int propertyIndex;
			private int methodIndex;
			private int methodParamsIndex;

			public readonly Type Current => current;

			public DependenciesEnumerator(ReflectionCache.InjectionInfo info)
			{
				this.info = info;
				current = null!;

				constructorParamIndex = 0;
				fieldIndex = 0;
				propertyIndex = 0;
				methodIndex = 0;
				methodParamsIndex = 0;
			}

			public bool MoveNext()
			{
				while (constructorParamIndex < info.ConstructorParams.Count)
				{
					current = info.ConstructorParams[constructorParamIndex++];
					return true;
				}
				while (fieldIndex < info.Fields.Count)
				{
					current = info.Fields[fieldIndex++].FieldType;
					return true;
				}
				while (propertyIndex < info.Properties.Count)
				{
					current = info.Properties[propertyIndex++].PropertyType;
					return true;
				}
				while (methodIndex < info.MethodParams.Count)
				{
					while (methodParamsIndex < info.MethodParams[methodIndex].Count)
					{
						current = info.MethodParams[methodIndex][methodParamsIndex++];
						return true;
					}
					methodIndex++;
					methodParamsIndex = 0;
				}
				return false;
			}

			public readonly DependenciesEnumerator GetEnumerator() => this;
		}
	}
}