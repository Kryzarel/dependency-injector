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
			if (HasCircularDependency(type, typeResolver, out Type? circType))
			{
				throw new CircularDependencyException($"Can't create object of type {type.FullName} because {circType?.FullName} has a circular dependency on it.");
			}

			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);

			if (info.Constructor != null)
			{
				int paramLength = info.ConstructorParams.Count;
				object[] constructorParams = GetFromParamCache(paramLength);
				for (int i = 0; i < paramLength; i++)
				{
					Type item = info.ConstructorParams[i];
					constructorParams[i] = typeResolver.GetObject(item);
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

		public void Inject(object obj, ITypeResolver typeResolver)
		{
			Type type = obj.GetType();
			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);

			for (int i = 0; i < info.Fields.Count; i++)
			{
				FieldInfo item = info.Fields[i];
				item.SetValue(obj, typeResolver.GetObject(item.FieldType));
			}

			for (int i = 0; i < info.Properties.Count; i++)
			{
				PropertyInfo item = info.Properties[i];
				item.SetValue(obj, typeResolver.GetObject(item.PropertyType));
			}

			for (int i = 0; i < info.Methods.Count; i++)
			{
				MethodInfo item = info.Methods[i];
				IReadOnlyList<Type> paramTypes = info.MethodParams[i];

				object[] methodParams = GetFromParamCache(paramTypes.Count);
				for (int j = 0; j < methodParams.Length; j++)
				{
					methodParams[j] = typeResolver.GetObject(paramTypes[j]);
				}
				item.Invoke(obj, methodParams);
				ReturnToParamCache(methodParams);
			}
		}

		public bool HasCircularDependency(Type type, ITypeResolver typeResolver)
		{
			return HasCircularDependency(type, typeResolver, out _);
		}

		public bool HasCircularDependency(Type type, ITypeResolver typeResolver, out Type? circType)
		{
			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);

			if (HasCircularDependency(info.ConstructorParams, x => x, type, typeResolver, out circType))
			{
				return true;
			}
			if (HasCircularDependency(info.Fields, x => x.FieldType, type, typeResolver, out circType))
			{
				return true;
			}
			if (HasCircularDependency(info.Properties, x => x.PropertyType, type, typeResolver, out circType))
			{
				return true;
			}
			for (int i = 0; i < info.Methods.Count; i++)
			{
				if (HasCircularDependency(info.MethodParams[i], x => x, type, typeResolver, out circType))
				{
					return true;
				}
			}
			return false;
		}

		private bool HasCircularDependency<T>(IReadOnlyList<T> list, Func<T, Type> getType, Type rootType, ITypeResolver typeResolver, out Type? circType)
		{
			circType = null;

			for (int i = 0; i < list.Count; i++)
			{
				Type t = getType(list[i]);
				Type type = typeResolver.TryGetType(t, out Type? resolvedType) ? resolvedType! : t;
				if (type == rootType || !circularDependencyTypes.Add(type) || HasCircularDependency(type, typeResolver, out circType))
				{
					circularDependencyTypes.Clear();
					circType ??= type;
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