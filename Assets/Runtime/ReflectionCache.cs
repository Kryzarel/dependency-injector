using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kryz.DI
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

			public InjectionInfo(ConstructorInfo? constructor,
				IReadOnlyList<Type> constructorParams,
				IReadOnlyList<FieldInfo> fields,
				IReadOnlyList<PropertyInfo> properties,
				IReadOnlyList<MethodInfo> methods,
				IReadOnlyList<IReadOnlyList<Type>> methodParams)
			{
				Constructor = constructor;
				ConstructorParams = constructorParams;
				Fields = fields;
				Properties = properties;
				Methods = methods;
				MethodParams = methodParams;
			}
		}

		private const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

		private static readonly Type injectAttribute = typeof(InjectAttribute);

		private readonly Dictionary<Type, InjectionInfo> cache = new();
		private readonly List<Type> constructorParams = new();
		private readonly List<FieldInfo> fields = new();
		private readonly List<PropertyInfo> properties = new();
		private readonly List<MethodInfo> methods = new();

		public InjectionInfo Get(Type type)
		{
			if (!cache.TryGetValue(type, out InjectionInfo info))
			{
				info = ProcessType(type);
				cache[type] = info;
			}
			return info;
		}

		private InjectionInfo ProcessType(Type type)
		{
			ConstructorInfo? constructor = GetInjectConstructor(type);
			GetConstructorParamTypes(constructor, constructorParams);

			GetMembersWithAttribute(type.GetFields(flags), injectAttribute, fields);
			GetMembersWithAttribute(type.GetProperties(flags), injectAttribute, properties);
			GetMembersWithAttribute(type.GetMethods(flags), injectAttribute, methods);

			InjectionInfo injectionInfo = new(
				constructor,
				GetArray(constructorParams),
				GetArray(fields),
				GetArray(properties),
				GetArray(methods),
				GetMethodParamTypes(methods));

			constructorParams.Clear();
			fields.Clear();
			properties.Clear();
			methods.Clear();

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

		private static void GetMembersWithAttribute<T>(IReadOnlyList<T> members, Type attribute, List<T> results) where T : MemberInfo
		{
			foreach (T item in members)
			{
				if (item.IsDefined(attribute))
				{
					results.Add(item);
				}
			}
		}

		private static T[] GetArray<T>(List<T> list)
		{
			return list.Count > 0 ? list.ToArray() : Array.Empty<T>();
		}
	}
}