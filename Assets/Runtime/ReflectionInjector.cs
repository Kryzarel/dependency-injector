using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Kryz.DI
{
	public class ReflectionInjector
	{
		private readonly ReflectionCache reflectionCache = new();

		public object CreateObject(Type type, ITypeResolver typeResolver)
		{
			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);

			if (info.Constructor != null)
			{
				object[] constructorParams = new object[info.ConstructorParams.Count];
				for (int i = 0; i < info.ConstructorParams.Count; i++)
				{
					Type item = info.ConstructorParams[i];
					constructorParams[i] = typeResolver.Get(item);
				}
				return info.Constructor.Invoke(constructorParams);
			}
			return FormatterServices.GetUninitializedObject(type);
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

				object[] objects = new object[paramTypes.Count];
				for (int j = 0; j < objects.Length; j++)
				{
					objects[j] = typeResolver.Get(paramTypes[j]);
				}
				item.Invoke(obj, objects);
			}
		}
	}
}