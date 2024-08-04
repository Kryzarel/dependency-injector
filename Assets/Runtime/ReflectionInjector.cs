using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Kryz.DI
{
	public class ReflectionInjector
	{
		private readonly ReflectionCache reflectionCache = new();
		// If you have a method with 32 parameters or more, kindly reconsider.
		private readonly object[][] paramCache = new object[32][];

		public ReflectionInjector()
		{
			paramCache[0] = Array.Empty<object>();
		}

		public object CreateObject(Type type, ITypeResolver typeResolver)
		{
			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);

			if (info.Constructor != null)
			{
				int paramLength = info.ConstructorParams.Count;
				object[] constructorParams = GetFromParamCache(paramLength);
				for (int i = 0; i < paramLength; i++)
				{
					Type item = info.ConstructorParams[i];
					constructorParams[i] = typeResolver.Get(item);
				}
				object obj = info.Constructor.Invoke(constructorParams);
				ReturnToParamCache(constructorParams);
				return obj;
			}
			else if (!type.IsAbstract)
			{
				return FormatterServices.GetUninitializedObject(type);
			}
			return null;
		}

		public void Inject(Type type, object obj, ITypeResolver typeResolver)
		{
			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);

			for (int i = 0; i < info.Fields.Count; i++)
			{
				FieldInfo item = info.Fields[i];
				item.SetValue(obj, typeResolver.Get(item.FieldType));
			}

			for (int i = 0; i < info.Properties.Count; i++)
			{
				PropertyInfo item = info.Properties[i];
				item.SetValue(obj, typeResolver.Get(item.PropertyType));
			}

			for (int i = 0; i < info.Methods.Count; i++)
			{
				MethodInfo item = info.Methods[i];
				IReadOnlyList<Type> paramTypes = info.MethodParams[i];

				object[] methodParams = GetFromParamCache(paramTypes.Count);
				for (int j = 0; j < methodParams.Length; j++)
				{
					methodParams[j] = typeResolver.Get(paramTypes[j]);
				}
				item.Invoke(obj, methodParams);
				ReturnToParamCache(methodParams);
			}
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