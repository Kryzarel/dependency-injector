using System;
using System.Collections.Generic;
using Kryz.DI.Reflection;

namespace Kryz.DI
{
	public class DependencyGraph
	{
		private readonly IReadOnlyDictionary<Type, Registration> registrations;

		private readonly ReflectionCache reflectionCache = new();
		private readonly Dictionary<Type, Type> circularDependencies = new();

		internal DependencyGraph(IReadOnlyDictionary<Type, Registration> registrations)
		{
			this.registrations = registrations;
			HashSet<Type> visitedTypes = new();

			foreach (Type type in registrations.Keys)
			{
				if (HasCircularDependency(type, visitedTypes, out Type? circular))
				{
					circularDependencies[type] = circular!;
				}
				visitedTypes.Clear();
			}
		}

		private bool HasCircularDependency(Type type, HashSet<Type> visitedTypes, out Type? circular)
		{
			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);
			visitedTypes.Add(type);

			foreach (Type dependency in new DependenciesEnumerator(info))
			{
				if (!visitedTypes.Add(dependency))
				{
					circular = dependency;
					return true;
				}

				if (registrations.TryGetValue(dependency, out Registration registration) && registration.Object == null)
				{
					if (HasCircularDependency(registration.Type, visitedTypes, out circular))
					{
						return true;
					}
				}
			}

			circular = null;
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