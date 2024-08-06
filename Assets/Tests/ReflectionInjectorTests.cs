using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Kryz.DI.Tests
{
	public class ReflectionInjectorTests
	{
		private class TypeResolver : ITypeResolver
		{
			private readonly Dictionary<Type, object> objects = new();

			public T Get<T>() => (T)objects[typeof(T)];

			public object Get(Type type) => objects[type];

			public bool TryGet<T>(out T? obj)
			{
				if (objects.TryGetValue(typeof(T), out object o))
				{
					obj = (T)o;
					return true;
				}
				obj = default;
				return false;
			}

			public bool TryGet(Type type, out object obj) => objects.TryGetValue(type, out obj);

			public void Add<T1, T2>(T2 obj) where T2 : notnull, T1 => objects[typeof(T1)] = obj;
		}

		[Test]
		public void TestCreate()
		{
			TypeResolver typeResolver = new();
			ReflectionInjector reflectionInjector = new();

			A a = (A)reflectionInjector.CreateObject(typeof(A), typeResolver);
			typeResolver.Add<A, A>(a);
			typeResolver.Add<IA, A>(a);

			B b = (B)reflectionInjector.CreateObject(typeof(B), typeResolver);
			typeResolver.Add<B, B>(b);
			typeResolver.Add<IB, B>(b);

			C c = (C)reflectionInjector.CreateObject(typeof(C), typeResolver);
			typeResolver.Add<C, C>(c);
			typeResolver.Add<IC, C>(c);

			D d = (D)reflectionInjector.CreateObject(typeof(D), typeResolver);
			typeResolver.Add<D, D>(d);
			typeResolver.Add<ID, D>(d);

			Empty empty = (Empty)reflectionInjector.CreateObject(typeof(Empty), typeResolver);

			Assert.AreEqual(a, b.A);

			Assert.AreEqual(a, c.A);
			Assert.AreEqual(b, c.B);

			Assert.AreEqual(null, d.A);
			Assert.AreEqual(null, d.B);
			Assert.AreEqual(null, d.C);

			Assert.AreNotEqual(null, empty);
		}

		[Test]
		public void TestCreateAndInject()
		{
			TypeResolver typeResolver = new();
			ReflectionInjector reflectionInjector = new();

			A a = (A)reflectionInjector.CreateObject(typeof(A), typeResolver);
			reflectionInjector.Inject(typeof(A), a, typeResolver);
			typeResolver.Add<A, A>(a);
			typeResolver.Add<IA, A>(a);

			B b = (B)reflectionInjector.CreateObject(typeof(B), typeResolver);
			reflectionInjector.Inject(typeof(B), b, typeResolver);
			typeResolver.Add<B, B>(b);
			typeResolver.Add<IB, B>(b);

			C c = (C)reflectionInjector.CreateObject(typeof(C), typeResolver);
			reflectionInjector.Inject(typeof(C), c, typeResolver);
			typeResolver.Add<C, C>(c);
			typeResolver.Add<IC, C>(c);

			D d = (D)reflectionInjector.CreateObject(typeof(D), typeResolver);
			reflectionInjector.Inject(typeof(D), d, typeResolver);
			typeResolver.Add<D, D>(d);
			typeResolver.Add<ID, D>(d);

			Assert.AreEqual(a, b.A);

			Assert.AreEqual(a, c.A);
			Assert.AreEqual(b, c.B);

			Assert.AreEqual(a, d.A);
			Assert.AreEqual(b, d.B);
			Assert.AreEqual(c, d.C);
		}
	}
}
