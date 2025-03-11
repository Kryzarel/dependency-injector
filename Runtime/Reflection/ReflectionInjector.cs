using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Kryz.DI.Exceptions;

namespace Kryz.DI.Reflection
{
	public class ReflectionInjector : IInjector
	{
		private readonly ReflectionCache reflectionCache = new();
		private readonly Dictionary<Type, IReadOnlyList<Type>> dependenciesCache = new();
		// If you have a method with 32 parameters or more, kindly reconsider.
		private readonly object[][] paramCache = new object[32][];

		public ReflectionInjector()
		{
			paramCache[0] = Array.Empty<object>();
		}

		public object CreateObject(Type type, IObjectResolver resolver)
		{
			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);

			if (info.Constructor != null)
			{
				int paramLength = info.ConstructorParams.Count;
				object[] constructorParams = GetFromParamCache(paramLength);
				for (int i = 0; i < paramLength; i++)
				{
					Type item = info.ConstructorParams[i];
					constructorParams[i] = resolver.GetObject(item);
				}
				object obj = info.Constructor.Invoke(constructorParams);
				ReturnToParamCache(constructorParams);
				return obj;
			}
			else if (!type.IsAbstract)
			{
				return FormatterServices.GetUninitializedObject(type);
			}
			throw new AbstractTypeException($"Can't create object of type {type.FullName} because it is abstract.");
		}

		public void Inject(object obj, IObjectResolver resolver)
		{
			Type type = obj.GetType();
			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);

			for (int i = 0; i < info.Fields.Count; i++)
			{
				FieldInfo item = info.Fields[i];
				item.SetValue(obj, resolver.GetObject(item.FieldType));
			}

			for (int i = 0; i < info.Properties.Count; i++)
			{
				PropertyInfo item = info.Properties[i];
				item.SetValue(obj, resolver.GetObject(item.PropertyType));
			}

			for (int i = 0; i < info.Methods.Count; i++)
			{
				MethodInfo item = info.Methods[i];
				IReadOnlyList<Type> paramTypes = info.MethodParams[i];

				object[] methodParams = GetFromParamCache(paramTypes.Count);
				for (int j = 0; j < methodParams.Length; j++)
				{
					methodParams[j] = resolver.GetObject(paramTypes[j]);
				}
				item.Invoke(obj, methodParams);
				ReturnToParamCache(methodParams);
			}
		}

		public IReadOnlyList<Type> GetDependencies(Type type)
		{
			if (!dependenciesCache.TryGetValue(type, out IReadOnlyList<Type> dependencies))
			{
				dependenciesCache[type] = dependencies = GetDependenciesList(type);
			}
			return dependencies;
		}

		private IReadOnlyList<Type> GetDependenciesList(Type type)
		{
			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);

			int count = info.ConstructorParams.Count + info.Fields.Count + info.Properties.Count;
			for (int i = 0; i < info.MethodParams.Count; i++)
			{
				count += info.MethodParams[i].Count;
			}

			Type[] dependencies = new Type[count];
			int index = 0;

			for (int i = 0; i < info.ConstructorParams.Count; i++)
			{
				dependencies[index++] = info.ConstructorParams[i];
			}

			for (int i = 0; i < info.Fields.Count; i++)
			{
				dependencies[index++] = info.Fields[i].FieldType;
			}

			for (int i = 0; i < info.Properties.Count; i++)
			{
				dependencies[index++] = info.Properties[i].PropertyType;
			}

			for (int i = 0; i < info.Methods.Count; i++)
			{
				IReadOnlyList<Type> paramTypes = info.MethodParams[i];

				for (int j = 0; j < paramTypes.Count; j++)
				{
					dependencies[index++] = paramTypes[j];
				}
			}

			return dependencies;
		}

		private object[] GetFromParamCache(int length)
		{
			return length < paramCache.Length ? (paramCache[length] ??= new object[length]) : new object[length];
		}

		private void ReturnToParamCache(object[] parameters)
		{
			int length = parameters.Length;
			if (length < paramCache.Length)
			{
				Array.Clear(parameters, 0, length);
			}
		}
	}
}