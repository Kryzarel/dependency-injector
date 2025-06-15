using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Kryz.DI.Reflection
{
	public class ExpressionInjector : IInjector
	{
		private delegate object CreateDelegate(IObjectResolver resolver);
		private delegate void InjectDelegate(object obj, IObjectResolver resolver);

		private static readonly MethodInfo getObjectMethod = typeof(IObjectResolver).GetMethod(nameof(IObjectResolver.GetObject), Type.EmptyTypes);

		private readonly ReflectionCache reflectionCache;
		private readonly Dictionary<Type, CreateDelegate> createCache = new();
		private readonly Dictionary<Type, InjectDelegate> injectCache = new();

		public ExpressionInjector(ReflectionCache? reflectionCache = null)
		{
			this.reflectionCache = reflectionCache ?? new();
		}

		public object CreateObject(Type type, IObjectResolver resolver)
		{
			if (!createCache.TryGetValue(type, out CreateDelegate create))
			{
				createCache[type] = create = CompileCreateExpression(type);
			}
			return create(resolver);
		}

		public void Inject(object obj, IObjectResolver resolver)
		{
			Type type = obj.GetType();
			if (!injectCache.TryGetValue(type, out InjectDelegate inject))
			{
				injectCache[type] = inject = CompileInjectExpression(type);
			}
			inject(obj, resolver);
		}

		public IReadOnlyList<Type> GetDependencies(Type type)
		{
			return reflectionCache.GetInfo(type).AllDependencies;
		}

		private class NullableRef<T>
		{
			public T? Value;
			public NullableRef(T value) => Value = value;
		}

		public void CacheAllClassesWithInjectAttribute()
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type type in assembly.GetTypes())
				{
					if (type.IsAbstract || type.IsGenericType)
					{
						continue;
					}

					if (type.IsDefined(typeof(InjectAttribute)))
					{
						createCache.TryAdd(type, CompileCreateExpression(type));
						injectCache.TryAdd(type, CompileInjectExpression(type));
					}
				}
			}
		}

		private CreateDelegate CompileCreateExpression(Type type)
		{
			ReflectionCache.InjectionInfo info = reflectionCache.GetInfo(type);

			// Final Expression:
			// object Create(IObjectResolver resolver)
			// {
			// 	return new Foo(resolver.GetObject(type1), resolver.GetObject(type2));
			// }

			ParameterExpression resolverParam = Expression.Parameter(typeof(IObjectResolver), "resolver");

			Expression[] constructorParams = new Expression[info.ConstructorParams.Count];
			for (int i = 0; i < constructorParams.Length; i++)
			{
				MethodInfo genericGetObject = getObjectMethod.MakeGenericMethod(info.ConstructorParams[i]);
				constructorParams[i] = Expression.Call(resolverParam, genericGetObject);
			}

			NewExpression constructor = Expression.New(info.Constructor, constructorParams);

			return Expression.Lambda<CreateDelegate>(constructor, resolverParam).Compile();
		}

		private InjectDelegate CompileInjectExpression(Type type)
		{
			ReflectionCache.InjectionInfo info = reflectionCache.GetInfo(type);

			// Final Expression:
			// object Inject(object obj, IObjectResolver resolver)
			// {
			// 	Foo foo = (Foo)obj;
			// 	foo.Var1 = resolver.GetObject(varType1);
			// 	foo.Var2 = resolver.GetObject(varType2);

			// 	foo.Property1 = resolver.GetObject(propType1);
			// 	foo.Property2 = resolver.GetObject(propType2);

			// 	foo.Method(resolver.GetObject(argType1), resolver.GetObject(argType1));
			// }

			ParameterExpression objParam = Expression.Parameter(typeof(object), "obj");
			ParameterExpression resolverParam = Expression.Parameter(typeof(IObjectResolver), "resolver");

			List<Expression> expressions = new(1 + info.Fields.Count + info.Properties.Count + info.Methods.Count);

			UnaryExpression castObj = Expression.Convert(objParam, type);

			expressions.Add(castObj);

			for (int i = 0; i < info.Fields.Count; i++)
			{
				FieldInfo item = info.Fields[i];
				MemberExpression variable = Expression.Field(castObj, item);
				MethodInfo genericGetObject = getObjectMethod.MakeGenericMethod(item.FieldType);
				MethodCallExpression getObject = Expression.Call(resolverParam, genericGetObject);
				expressions.Add(Expression.Assign(variable, getObject));
			}

			for (int i = 0; i < info.Properties.Count; i++)
			{
				PropertyInfo item = info.Properties[i];
				MemberExpression property = Expression.MakeMemberAccess(castObj, item);
				MethodInfo genericGetObject = getObjectMethod.MakeGenericMethod(item.PropertyType);
				MethodCallExpression getObject = Expression.Call(resolverParam, genericGetObject);
				expressions.Add(Expression.Assign(property, getObject));
			}

			for (int i = 0; i < info.Methods.Count; i++)
			{
				MethodInfo item = info.Methods[i];
				IReadOnlyList<Type> paramTypes = info.MethodParams[i];

				Expression[] methodParams = new Expression[paramTypes.Count];
				for (int j = 0; j < paramTypes.Count; j++)
				{
					MethodInfo genericGetObject = getObjectMethod.MakeGenericMethod(paramTypes[j]);
					methodParams[j] = Expression.Call(resolverParam, genericGetObject);
				}
				expressions.Add(Expression.Call(castObj, item, methodParams));
			}

			BlockExpression body = Expression.Block(expressions);

			return Expression.Lambda<InjectDelegate>(body, objParam, resolverParam).Compile();
		}
	}
}