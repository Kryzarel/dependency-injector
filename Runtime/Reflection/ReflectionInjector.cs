using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Kryz.DI.Reflection
{
	/// <summary>
	/// <para>This class should handle everything that requires reflection in the dependency injection framework.</para>
	/// <para>No other class in this assembly, other than <see cref="ReflectionCache"/>, should use the <see cref="System.Reflection"/> namespace.</para>
	/// </summary>
	public class ReflectionInjector : IInjector
	{
		private readonly ReflectionCache reflectionCache = new();
		// If you have a method with 32 parameters or more, kindly reconsider.
		private readonly object[][] paramCache = new object[32][];
		private readonly HashSet<Type> circularDependencyTypes = new();

		public ReflectionInjector()
		{
			paramCache[0] = Array.Empty<object>();
		}

		public object CreateObject(Type type, IResolver typeResolver)
		{
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

		public void Inject(object obj, IResolver typeResolver)
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

		public bool HasCircularDependency(Type type, IResolver typeResolver)
		{
			return HasCircularDependency(type, typeResolver, out _);
		}

		public bool HasCircularDependency(Type type, IResolver typeResolver, out Type? circType)
		{
			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);
			circularDependencyTypes.Add(type);

			foreach (Type dependency in new DependenciesEnumerable(info))
			{
				if (!circularDependencyTypes.Add(dependency))
				{
					circularDependencyTypes.Clear();
					circType = dependency;
					return true;
				}

				Type recurseType = typeResolver.TryGetType(dependency, out Type? t) ? t! : dependency;
				if (HasCircularDependency(recurseType, typeResolver, out circType))
				{
					circularDependencyTypes.Clear();
					return true;
				}
			}

			circularDependencyTypes.Clear();
			circType = null;
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

		private readonly struct DependenciesEnumerable
		{
			private readonly ReflectionCache.InjectionInfo info;
			public DependenciesEnumerable(ReflectionCache.InjectionInfo info) => this.info = info;
			public DependenciesEnumerator GetEnumerator() => new(info);
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
		}
	}
}