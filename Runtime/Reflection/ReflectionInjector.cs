using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Kryz.DI.Exceptions;
using Kryz.Utils;

namespace Kryz.DI.Reflection
{
	public class ReflectionInjector : IInjector
	{
		private readonly ReflectionCache reflectionCache;
		private readonly ExactSizeArrayPool<object> arrayPool;

		public ReflectionInjector(ReflectionCache? reflectionCache = null)
		{
			arrayPool = ExactSizeArrayPool<object>.Shared;
			this.reflectionCache = reflectionCache ?? new();
		}

		public object CreateObject(Type type, IObjectResolver resolver)
		{
			ReflectionCache.InjectionInfo info = reflectionCache.GetInfo(type);

			if (info.Constructor != null)
			{
				int paramLength = info.ConstructorParams.Count;
				object[] constructorParams = arrayPool.Rent(paramLength);
				for (int i = 0; i < paramLength; i++)
				{
					Type item = info.ConstructorParams[i];
					constructorParams[i] = resolver.ResolveObject(item);
				}
				object obj = info.Constructor.Invoke(constructorParams);
				arrayPool.Return(constructorParams, clearArray: true);
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
			ReflectionCache.InjectionInfo info = reflectionCache.GetInfo(type);

			for (int i = 0; i < info.Fields.Count; i++)
			{
				FieldInfo item = info.Fields[i];
				item.SetValue(obj, resolver.ResolveObject(item.FieldType));
			}

			for (int i = 0; i < info.Properties.Count; i++)
			{
				PropertyInfo item = info.Properties[i];
				item.SetValue(obj, resolver.ResolveObject(item.PropertyType));
			}

			for (int i = 0; i < info.Methods.Count; i++)
			{
				MethodInfo item = info.Methods[i];
				IReadOnlyList<Type> paramTypes = info.MethodParams[i];

				object[] methodParams = arrayPool.Rent(paramTypes.Count);
				for (int j = 0; j < methodParams.Length; j++)
				{
					methodParams[j] = resolver.ResolveObject(paramTypes[j]);
				}
				item.Invoke(obj, methodParams);
				arrayPool.Return(methodParams, clearArray: true);
			}
		}

		public IReadOnlyList<Type> GetDependencies(Type type)
		{
			return reflectionCache.GetInfo(type).AllDependencies;
		}
	}
}