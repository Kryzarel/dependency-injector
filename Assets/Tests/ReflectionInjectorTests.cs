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

		[Test]
		public void TestCircularDependency()
		{
			ReflectionInjector reflectionInjector = new();

			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IA)));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IB)));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IC)));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ID)));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IE)));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IGeneric<IA, IB, IC>)));

			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(ICircular1)));
			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(ICircular2)));

			// These don't have the [Inject] attribute, so the circular dependency won't be found
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ICircular1NoInject)));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ICircular2NoInject)));

			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(A)));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(B)));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(C)));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(D)));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(E)));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(Generic<IA, IB, IC>)));

			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular1)));
			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular2)));

			// These use the interfaces that don't have the [Inject] attribute, so the circular dependency won't be found
			Assert.False(reflectionInjector.HasCircularDependency(typeof(Circular1NoInject)));
			Assert.False(reflectionInjector.HasCircularDependency(typeof(Circular2NoInject)));

			// These depend on the concrete class, so the circular dependency WILL be found
			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular1Concrete)));
			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular2Concrete)));
		}
	}
}
