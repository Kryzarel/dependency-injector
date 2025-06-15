using System;
using System.Collections.Generic;
using System.Reflection;
using Kryz.Utils;

namespace Kryz.DI.Reflection
{
	public class ReflectionCache
	{
		public class InjectionInfo
		{
			public readonly ConstructorInfo? Constructor;
			public readonly IReadOnlyList<Type> ConstructorParams;
			public readonly IReadOnlyList<FieldInfo> Fields;
			public readonly IReadOnlyList<PropertyInfo> Properties;
			public readonly IReadOnlyList<MethodInfo> Methods;
			public readonly IReadOnlyList<IReadOnlyList<Type>> MethodParams;
			public readonly IReadOnlyList<Type> AllDependencies;

			public InjectionInfo(ConstructorInfo? constructor,
				IReadOnlyList<Type> constructorParams,
				IReadOnlyList<FieldInfo> fields,
				IReadOnlyList<PropertyInfo> properties,
				IReadOnlyList<MethodInfo> methods,
				IReadOnlyList<IReadOnlyList<Type>> methodParams,
				IReadOnlyList<Type> allDependencies)
			{
				Constructor = constructor;
				ConstructorParams = constructorParams;
				Fields = fields;
				Properties = properties;
				Methods = methods;
				MethodParams = methodParams;
				AllDependencies = allDependencies;
			}
		}

		private const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

		private static readonly Type injectAttribute = typeof(InjectAttribute);

		private readonly Dictionary<Type, InjectionInfo> cache = new();
		private readonly List<Type> constructorParams = new();
		private readonly List<FieldInfo> fields = new();
		private readonly List<PropertyInfo> properties = new();
		private readonly List<MethodInfo> methods = new();
		private readonly HashSet<Type> allDependencies = new();

		public InjectionInfo GetInfo(Type type)
		{
			if (!cache.TryGetValue(type, out InjectionInfo info))
			{
				cache[type] = info = ProcessType(type);
			}
			return info;
		}

		private InjectionInfo ProcessType(Type type)
		{
			ConstructorInfo? constructor = GetInjectConstructor(type);
			GetConstructorParamTypes(constructor, constructorParams);

			type.GetAllFieldsWithAttribute(flags, injectAttribute, fields);
			type.GetAllPropertiesWithAttribute(flags, injectAttribute, properties);
			type.GetAllMethodsWithAttribute(flags, injectAttribute, methods);

			IReadOnlyList<IReadOnlyList<Type>> methodParams = GetMethodParamTypes(methods);
			GetAllDependencies(constructorParams, fields, properties, methods, methodParams, allDependencies);

			InjectionInfo injectionInfo = new(
				constructor,
				GetArray(constructorParams),
				GetArray(fields),
				GetArray(properties),
				GetArray(methods),
				methodParams,
				GetArray(allDependencies));

			constructorParams.Clear();
			fields.Clear();
			properties.Clear();
			methods.Clear();
			allDependencies.Clear();

			return injectionInfo;
		}

		private static ConstructorInfo? GetInjectConstructor(Type type)
		{
			ConstructorInfo[] constructors = type.GetConstructors(flags);

			if (constructors.Length == 1)
			{
				return constructors[0];
			}
			else if (constructors.Length > 1)
			{
				foreach (ConstructorInfo item in constructors)
				{
					if (item.IsDefined(injectAttribute))
					{
						return item;
					}
				}
			}
			return null;
		}

		private static void GetConstructorParamTypes(ConstructorInfo? constructor, List<Type> constructorParams)
		{
			if (constructor != null)
			{
				ParameterInfo[] parameters = constructor.GetParameters();
				if (constructorParams.Capacity < parameters.Length)
				{
					constructorParams.Capacity = parameters.Length;
				}

				foreach (ParameterInfo item in parameters)
				{
					constructorParams.Add(item.ParameterType);
				}
			}
		}

		private static Type[][] GetMethodParamTypes(IReadOnlyList<MethodInfo> methods)
		{
			Type[][] methodParams = new Type[methods.Count][];
			for (int i = 0; i < methodParams.Length; i++)
			{
				ParameterInfo[] parameters = methods[i].GetParameters();
				methodParams[i] = new Type[parameters.Length];

				for (int j = 0; j < methodParams[i].Length; j++)
				{
					methodParams[i][j] = parameters[j].ParameterType;
				}
			}
			return methodParams;
		}

		private static void GetAllDependencies(
			IReadOnlyList<Type> constructorParams,
			IReadOnlyList<FieldInfo> fields,
			IReadOnlyList<PropertyInfo> properties,
			IReadOnlyList<MethodInfo> methods,
			IReadOnlyList<IReadOnlyList<Type>> methodParams,
			HashSet<Type> allDependencies)
		{
			int count = constructorParams.Count + fields.Count + properties.Count;
			for (int i = 0; i < methodParams.Count; i++)
			{
				count += methodParams[i].Count;
			}

			allDependencies.EnsureCapacity(count);

			for (int i = 0; i < constructorParams.Count; i++)
			{
				allDependencies.Add(constructorParams[i]);
			}

			for (int i = 0; i < fields.Count; i++)
			{
				allDependencies.Add(fields[i].FieldType);
			}

			for (int i = 0; i < properties.Count; i++)
			{
				allDependencies.Add(properties[i].PropertyType);
			}

			for (int i = 0; i < methods.Count; i++)
			{
				IReadOnlyList<Type> paramTypes = methodParams[i];

				for (int j = 0; j < paramTypes.Count; j++)
				{
					allDependencies.Add(paramTypes[j]);
				}
			}
		}

		private static T[] GetArray<T>(List<T> list)
		{
			return list.Count > 0 ? list.ToArray() : Array.Empty<T>();
		}

		private static T[] GetArray<T>(HashSet<T> set)
		{
			return set.Count > 0 ? set.ToArray() : Array.Empty<T>();
		}
	}
}