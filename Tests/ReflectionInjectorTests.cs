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
			private readonly Dictionary<Type, Type> registrations = new();

			public T GetObject<T>() => (T)objects[typeof(T)];
			public object GetObject(Type type) => objects[type];

			public bool TryGetObject<T>(out T? obj)
			{
				bool result = objects.TryGetValue(typeof(T), out object o);
				obj = result ? (T)o : default;
				return result;
			}
			public bool TryGetObject(Type type, out object? obj) => objects.TryGetValue(type, out obj);

			public Type GetType<T>() => registrations[typeof(T)];
			public Type GetType(Type type) => registrations[type];

			public bool TryGetType<T>(out Type? type) => registrations.TryGetValue(typeof(T), out type);
			public bool TryGetType(Type type, out Type? resolvedType) => registrations.TryGetValue(type, out resolvedType);

			public void Add<T1, T2>(T2 obj) where T2 : notnull, T1
			{
				objects[typeof(T1)] = obj;
				registrations[typeof(T1)] = typeof(T2);
			}

			public void Add<T1, T2>() where T2 : T1 => registrations[typeof(T1)] = typeof(T2);
		}

		private static void Create(TypeResolver typeResolver, ReflectionInjector reflectionInjector, bool inject, out A a, out B b, out C c, out D d, out E e, out Empty empty, out Generic<IA, IB, IC> generic, out Generic<ID, IE, Empty> generic2)
		{
			a = (A)reflectionInjector.CreateObject(typeof(A), typeResolver);
			if (inject) reflectionInjector.Inject(a, typeResolver);
			typeResolver.Add<A, A>(a);
			typeResolver.Add<IA, A>(a);

			b = (B)reflectionInjector.CreateObject(typeof(B), typeResolver);
			if (inject) reflectionInjector.Inject(b, typeResolver);
			typeResolver.Add<B, B>(b);
			typeResolver.Add<IB, B>(b);

			c = (C)reflectionInjector.CreateObject(typeof(C), typeResolver);
			if (inject) reflectionInjector.Inject(c, typeResolver);
			typeResolver.Add<C, C>(c);
			typeResolver.Add<IC, C>(c);

			d = (D)reflectionInjector.CreateObject(typeof(D), typeResolver);
			if (inject) reflectionInjector.Inject(d, typeResolver);
			typeResolver.Add<D, D>(d);
			typeResolver.Add<ID, D>(d);

			e = (E)reflectionInjector.CreateObject(typeof(E), typeResolver);
			if (inject) reflectionInjector.Inject(e, typeResolver);
			typeResolver.Add<E, E>(e);
			typeResolver.Add<IE, E>(e);

			empty = (Empty)reflectionInjector.CreateObject(typeof(Empty), typeResolver);
			if (inject) reflectionInjector.Inject(empty, typeResolver);
			typeResolver.Add<Empty, Empty>(empty);

			generic = (Generic<IA, IB, IC>)reflectionInjector.CreateObject(typeof(Generic<IA, IB, IC>), typeResolver);
			if (inject) reflectionInjector.Inject(generic, typeResolver);

			generic2 = (Generic<ID, IE, Empty>)reflectionInjector.CreateObject(typeof(Generic<ID, IE, Empty>), typeResolver);
			if (inject) reflectionInjector.Inject(generic2, typeResolver);
		}

		[Test]
		public void TestCreate()
		{
			TypeResolver typeResolver = new();
			ReflectionInjector reflectionInjector = new();

			Create(typeResolver, reflectionInjector, inject: false, out A a, out B b, out C c, out D d, out E e, out Empty empty, out Generic<IA, IB, IC> generic, out Generic<ID, IE, Empty> generic2);

			Assert.AreEqual(a, b.A);

			Assert.AreEqual(a, c.A);
			Assert.AreEqual(b, c.B);

			Assert.AreEqual(null, d.A);
			Assert.AreEqual(null, d.B);
			Assert.AreEqual(null, d.C);

			Assert.AreEqual(null, e.A);
			Assert.AreEqual(null, e.B);
			Assert.AreEqual(null, e.C);
			Assert.AreEqual(null, e.D);

			Assert.AreNotEqual(null, empty);

			Assert.AreEqual(null, generic.One);
			Assert.AreEqual(null, generic.Two);
			Assert.AreEqual(null, generic.Three);

			Assert.AreEqual(null, generic2.One);
			Assert.AreEqual(null, generic2.Two);
			Assert.AreEqual(null, generic2.Three);
		}

		[Test]
		public void TestCreateAndInject()
		{
			TypeResolver typeResolver = new();
			ReflectionInjector reflectionInjector = new();

			Create(typeResolver, reflectionInjector, inject: true, out A a, out B b, out C c, out D d, out E e, out Empty empty, out Generic<IA, IB, IC> generic, out Generic<ID, IE, Empty> generic2);

			Assert.AreEqual(a, b.A);

			Assert.AreEqual(a, c.A);
			Assert.AreEqual(b, c.B);

			Assert.AreEqual(a, d.A);
			Assert.AreEqual(b, d.B);
			Assert.AreEqual(c, d.C);

			Assert.AreEqual(a, e.A);
			Assert.AreEqual(b, e.B);
			Assert.AreEqual(c, e.C);
			Assert.AreEqual(d, e.D);

			Assert.AreNotEqual(null, empty);

			Assert.AreEqual(a, generic.One);
			Assert.AreEqual(b, generic.Two);
			Assert.AreEqual(c, generic.Three);

			Assert.AreEqual(d, generic2.One);
			Assert.AreEqual(e, generic2.Two);
			Assert.AreEqual(empty, generic2.Three);
		}

		[Test]
		public void TestProtectedSubClassInject()
		{
			TypeResolver typeResolver = new();
			ReflectionInjector reflectionInjector = new();

			Create(typeResolver, reflectionInjector, inject: true, out A a, out B b, out C c, out D d, out E e, out Empty empty, out Generic<IA, IB, IC> generic, out Generic<ID, IE, Empty> generic2);

			{
				ProtectedSubClassInject<IA> subClass = new();
				reflectionInjector.Inject(subClass, typeResolver);
				Assert.AreEqual(a, subClass.Value);
			}

			{
				BaseClass<IA> subClassCastToBase = new ProtectedSubClassInject<IA>();
				reflectionInjector.Inject(subClassCastToBase, typeResolver);
				Assert.AreEqual(a, subClassCastToBase.Value);
			}
		}

		[Test]
		public void TestCircularDependency()
		{
			TypeResolver typeResolver = new();
			ReflectionInjector reflectionInjector = new();

			typeResolver.Add<IA, A>();
			typeResolver.Add<IB, B>();
			typeResolver.Add<IC, C>();
			typeResolver.Add<ID, D>();
			typeResolver.Add<IE, E>();
			typeResolver.Add<ICircular1, Circular1>();
			typeResolver.Add<ICircular2, Circular2>();
			typeResolver.Add<ICircular1NoInject, Circular1NoInject>();
			typeResolver.Add<ICircular2NoInject, Circular2NoInject>();

			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IA), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IB), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IC), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ID), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IE), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IGeneric<IA, IB, IC>), typeResolver));

			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(ICircular1), typeResolver));
			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(ICircular2), typeResolver));

			// These don't have the [Inject] attribute, so the circular dependency won't be found
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ICircular1NoInject), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ICircular2NoInject), typeResolver));

			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(A), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(B), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(C), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(D), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(E), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(Generic<IA, IB, IC>), typeResolver));

			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular1), typeResolver));
			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular2), typeResolver));

			// Even though these use the interfaces that don't have the [Inject] attribute, given the registration, the circular dependency WILL be found
			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular1NoInject), typeResolver));
			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular2NoInject), typeResolver));

			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular1Concrete), typeResolver));
			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular2Concrete), typeResolver));
		}

		[Test]
		public void TestCircularDependencyWithoutRegistration()
		{
			TypeResolver typeResolver = new();
			ReflectionInjector reflectionInjector = new();

			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IA), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IB), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IC), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ID), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IE), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IGeneric<IA, IB, IC>), typeResolver));

			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(ICircular1), typeResolver));
			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(ICircular2), typeResolver));

			// These don't have the [Inject] attribute, so the circular dependency won't be found
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ICircular1NoInject), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ICircular2NoInject), typeResolver));

			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(A), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(B), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(C), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(D), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(E), typeResolver));
			Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(Generic<IA, IB, IC>), typeResolver));

			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular1), typeResolver));
			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular2), typeResolver));

			// These use the interfaces that don't have the [Inject] attribute, so the circular dependency won't be found
			Assert.False(reflectionInjector.HasCircularDependency(typeof(Circular1NoInject), typeResolver));
			Assert.False(reflectionInjector.HasCircularDependency(typeof(Circular2NoInject), typeResolver));

			// These depend on the concrete class, so the circular dependency WILL be found
			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular1Concrete), typeResolver));
			Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular2Concrete), typeResolver));
		}
	}
}
