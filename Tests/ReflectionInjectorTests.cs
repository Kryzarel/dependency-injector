using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Kryz.DI.Reflection;
using NUnit.Framework;

namespace Kryz.DI.Tests
{
	public class ReflectionInjectorTests
	{
		private class Resolver : IObjectResolver, ITypeResolver
		{
			private readonly Dictionary<Type, object> objects = new();
			private readonly Dictionary<Type, Type> registrations = new();

			public T GetObject<T>() => (T)objects[typeof(T)];
			public object GetObject(Type type) => objects[type];

			public bool TryGetObject<T>([MaybeNullWhen(returnValue: false)] out T obj)
			{
				bool result = objects.TryGetValue(typeof(T), out object o);
				obj = result ? (T)o : default;
				return result;
			}
			public bool TryGetObject(Type type, [MaybeNullWhen(returnValue: false)] out object obj) => objects.TryGetValue(type, out obj);

			public Type GetType<T>() => registrations[typeof(T)];
			public Type GetType(Type type) => registrations[type];

			public bool TryGetType<T>([MaybeNullWhen(returnValue: false)] out Type type) => registrations.TryGetValue(typeof(T), out type);
			public bool TryGetType(Type type, [MaybeNullWhen(returnValue: false)] out Type resolvedType) => registrations.TryGetValue(type, out resolvedType);

			public void Add<T1, T2>(T2 obj) where T2 : notnull, T1
			{
				objects[typeof(T1)] = obj;
				registrations[typeof(T1)] = typeof(T2);
			}

			public void Add<T1, T2>() where T2 : T1 => registrations[typeof(T1)] = typeof(T2);
		}

		private static void Create(Resolver resolver, ReflectionInjector reflectionInjector, bool inject, out A a, out B b, out C c, out D d, out E e, out Empty empty, out Generic<IA, IB, IC> generic, out Generic<ID, IE, Empty> generic2)
		{
			a = (A)reflectionInjector.CreateObject(typeof(A), resolver);
			if (inject) reflectionInjector.Inject(a, resolver);
			resolver.Add<A, A>(a);
			resolver.Add<IA, A>(a);

			b = (B)reflectionInjector.CreateObject(typeof(B), resolver);
			if (inject) reflectionInjector.Inject(b, resolver);
			resolver.Add<B, B>(b);
			resolver.Add<IB, B>(b);

			c = (C)reflectionInjector.CreateObject(typeof(C), resolver);
			if (inject) reflectionInjector.Inject(c, resolver);
			resolver.Add<C, C>(c);
			resolver.Add<IC, C>(c);

			d = (D)reflectionInjector.CreateObject(typeof(D), resolver);
			if (inject) reflectionInjector.Inject(d, resolver);
			resolver.Add<D, D>(d);
			resolver.Add<ID, D>(d);

			e = (E)reflectionInjector.CreateObject(typeof(E), resolver);
			if (inject) reflectionInjector.Inject(e, resolver);
			resolver.Add<E, E>(e);
			resolver.Add<IE, E>(e);

			empty = (Empty)reflectionInjector.CreateObject(typeof(Empty), resolver);
			if (inject) reflectionInjector.Inject(empty, resolver);
			resolver.Add<Empty, Empty>(empty);

			generic = (Generic<IA, IB, IC>)reflectionInjector.CreateObject(typeof(Generic<IA, IB, IC>), resolver);
			if (inject) reflectionInjector.Inject(generic, resolver);

			generic2 = (Generic<ID, IE, Empty>)reflectionInjector.CreateObject(typeof(Generic<ID, IE, Empty>), resolver);
			if (inject) reflectionInjector.Inject(generic2, resolver);
		}

		[Test]
		public void TestCreate()
		{
			Resolver resolver = new();
			ReflectionInjector reflectionInjector = new();

			Create(resolver, reflectionInjector, inject: false, out A a, out B b, out C c, out D d, out E e, out Empty empty, out Generic<IA, IB, IC> generic, out Generic<ID, IE, Empty> generic2);

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
			Resolver resolver = new();
			ReflectionInjector reflectionInjector = new();

			Create(resolver, reflectionInjector, inject: true, out A a, out B b, out C c, out D d, out E e, out Empty empty, out Generic<IA, IB, IC> generic, out Generic<ID, IE, Empty> generic2);

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
			Resolver resolver = new();
			ReflectionInjector reflectionInjector = new();

			Create(resolver, reflectionInjector, inject: true, out A a, out B b, out C c, out D d, out E e, out Empty empty, out Generic<IA, IB, IC> generic, out Generic<ID, IE, Empty> generic2);

			{
				ProtectedSubClassInject<IA> subClass = new();
				reflectionInjector.Inject(subClass, resolver);
				Assert.AreEqual(a, subClass.Value);
			}

			{
				BaseClass<IA> subClassCastToBase = new ProtectedSubClassInject<IA>();
				reflectionInjector.Inject(subClassCastToBase, resolver);
				Assert.AreEqual(a, subClassCastToBase.Value);
			}
		}

		// [Test]
		// public void TestCircularDependency()
		// {
		// 	TypeResolver resolver = new();
		// 	ReflectionInjector reflectionInjector = new();

		// 	resolver.Add<IA, A>();
		// 	resolver.Add<IB, B>();
		// 	resolver.Add<IC, C>();
		// 	resolver.Add<ID, D>();
		// 	resolver.Add<IE, E>();
		// 	resolver.Add<ICircular1, Circular1>();
		// 	resolver.Add<ICircular2, Circular2>();
		// 	resolver.Add<ICircular1NoInject, Circular1NoInject>();
		// 	resolver.Add<ICircular2NoInject, Circular2NoInject>();

		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IA), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IB), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IC), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ID), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IE), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IGeneric<IA, IB, IC>), resolver));

		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(ICircular1), resolver));
		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(ICircular2), resolver));

		// 	// These don't have the [Inject] attribute, so the circular dependency won't be found
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ICircular1NoInject), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ICircular2NoInject), resolver));

		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(A), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(B), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(C), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(D), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(E), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(Generic<IA, IB, IC>), resolver));

		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular1), resolver));
		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular2), resolver));

		// 	// Even though these use the interfaces that don't have the [Inject] attribute, given the registration, the circular dependency WILL be found
		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular1NoInject), resolver));
		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular2NoInject), resolver));

		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular1Concrete), resolver));
		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular2Concrete), resolver));
		// }

		// [Test]
		// public void TestCircularDependencyWithoutRegistration()
		// {
		// 	TypeResolver resolver = new();
		// 	ReflectionInjector reflectionInjector = new();

		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IA), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IB), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IC), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ID), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IE), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(IGeneric<IA, IB, IC>), resolver));

		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(ICircular1), resolver));
		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(ICircular2), resolver));

		// 	// These don't have the [Inject] attribute, so the circular dependency won't be found
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ICircular1NoInject), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(ICircular2NoInject), resolver));

		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(A), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(B), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(C), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(D), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(E), resolver));
		// 	Assert.IsFalse(reflectionInjector.HasCircularDependency(typeof(Generic<IA, IB, IC>), resolver));

		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular1), resolver));
		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular2), resolver));

		// 	// These use the interfaces that don't have the [Inject] attribute, so the circular dependency won't be found
		// 	Assert.False(reflectionInjector.HasCircularDependency(typeof(Circular1NoInject), resolver));
		// 	Assert.False(reflectionInjector.HasCircularDependency(typeof(Circular2NoInject), resolver));

		// 	// These depend on the concrete class, so the circular dependency WILL be found
		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular1Concrete), resolver));
		// 	Assert.IsTrue(reflectionInjector.HasCircularDependency(typeof(Circular2Concrete), resolver));
		// }
	}
}
