using System;
using System.Collections.Generic;
using System.Reflection;
using Kryz.Collections;
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

		public InjectionInfo GetInfo(Type type)
		{
			if (!TryGetFromCache(type, out InjectionInfo info))
			{
				info = ProcessType(type);
				AddToCache(type, info);
			}
			return info;
		}

		private bool TryGetFromCache(Type type, out InjectionInfo info)
		{
			lock (cache)
			{
				return cache.TryGetValue(type, out info);
			}
		}

		private void AddToCache(Type type, InjectionInfo info)
		{
			lock (cache)
			{
				cache[type] = info;
			}
		}

		private InjectionInfo ProcessType(Type type)
		{
			ConstructorInfo? constructor = GetInjectConstructor(type);
			IReadOnlyList<Type> constructorParams = GetConstructorParamTypes(constructor);

			using PooledList<FieldInfo> fields = PooledList<FieldInfo>.Rent();
			using PooledList<PropertyInfo> properties = PooledList<PropertyInfo>.Rent();
			using PooledList<MethodInfo> methods = PooledList<MethodInfo>.Rent();

			type.GetAllFieldsWithAttribute(flags, injectAttribute, fields);
			type.GetAllPropertiesWithAttribute(flags, injectAttribute, properties);
			type.GetAllMethodsWithAttribute(flags, injectAttribute, methods);

			IReadOnlyList<IReadOnlyList<Type>> methodParams = GetMethodParamTypes(methods);

			int count = constructorParams.Count + fields.Count + properties.Count;
			for (int i = 0; i < methodParams.Count; i++)
			{
				count += methodParams[i].Count;
			}
			using PooledList<Type> allDependencies = PooledList<Type>.Rent(count);
			GetAllDependencies(constructorParams, fields, properties, methodParams, allDependencies);

			InjectionInfo injectionInfo = new(
				constructor,
				constructorParams,
				fields.ToArray(),
				properties.ToArray(),
				methods.ToArray(),
				methodParams,
				allDependencies.ToArray());

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

		private static Type[] GetConstructorParamTypes(ConstructorInfo? constructor)
		{
			if (constructor == null)
			{
				return Array.Empty<Type>();
			}

			ParameterInfo[] parameters = constructor.GetParameters();
			Type[] constructorParams = parameters.Length == 0 ? Array.Empty<Type>() : new Type[parameters.Length];

			for (int i = 0; i < parameters.Length; i++)
			{
				constructorParams[i] = parameters[i].ParameterType;
			}
			return constructorParams;
		}

		private static Type[][] GetMethodParamTypes(IReadOnlyList<MethodInfo> methods)
		{
			Type[][] methodParams = methods.Count == 0 ? Array.Empty<Type[]>() : new Type[methods.Count][];
			for (int i = 0; i < methodParams.Length; i++)
			{
				ParameterInfo[] parameters = methods[i].GetParameters();
				methodParams[i] = parameters.Length == 0 ? Array.Empty<Type>() : new Type[parameters.Length];

				for (int j = 0; j < parameters.Length; j++)
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
			IReadOnlyList<IReadOnlyList<Type>> methodParams,
			IList<Type> allDependencies)
		{
			for (int i = 0; i < constructorParams.Count; i++)
			{
				Type item = constructorParams[i];
				if (!allDependencies.Contains(item))
				{
					allDependencies.Add(item);
				}
			}

			for (int i = 0; i < fields.Count; i++)
			{
				Type item = fields[i].FieldType;
				if (!allDependencies.Contains(item))
				{
					allDependencies.Add(item);
				}
			}

			for (int i = 0; i < properties.Count; i++)
			{
				Type item = properties[i].PropertyType;
				if (!allDependencies.Contains(item))
				{
					allDependencies.Add(item);
				}
			}

			for (int i = 0; i < methodParams.Count; i++)
			{
				IReadOnlyList<Type> paramTypes = methodParams[i];

				for (int j = 0; j < paramTypes.Count; j++)
				{
					Type item = paramTypes[j];
					if (!allDependencies.Contains(item))
					{
						allDependencies.Add(item);
					}
				}
			}
		}
	}
}