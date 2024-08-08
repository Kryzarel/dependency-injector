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
		private readonly HashSet<Type> circularDependencyTypes = new();

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
			throw new InjectionException($"Can't create object of type {type.FullName} because it is abstract.");
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

		public bool HasCircularDependency(Type type)
		{
			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);

			if (HasCircularDependency(info.ConstructorParams, x => x, type))
			{
				return true;
			}
			if (HasCircularDependency(info.Fields, x => x.FieldType, type))
			{
				return true;
			}
			if (HasCircularDependency(info.Properties, x => x.PropertyType, type))
			{
				return true;
			}
			for (int i = 0; i < info.Methods.Count; i++)
			{
				if (HasCircularDependency(info.MethodParams[i], x => x, type))
				{
					return true;
				}
			}
			return false;
		}

		private bool HasCircularDependency<T>(IReadOnlyList<T> list, Func<T, Type> getType, Type rootType)
		{
			for (int i = 0; i < list.Count; i++)
			{
				Type type = getType(list[i]);
				if (type == rootType || !circularDependencyTypes.Add(type) || HasCircularDependency(type))
				{
					circularDependencyTypes.Clear();
					return true;
				}
				circularDependencyTypes.Clear();
			}
			return false;
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