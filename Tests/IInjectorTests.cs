using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Kryz.DI.Reflection;
using NUnit.Framework;

namespace Kryz.DI.Tests
{
	public class IInjectorTests
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

		public static IEnumerable<IInjector> Injectors()
		{
			yield return new ReflectionInjector();
			yield return new ExpressionInjector();
		}

		private static void Create(Resolver resolver, IInjector injector, bool inject, out A a, out B b, out C c, out D d, out E e, out EmptyClass empty, out Generic<IA, IB, IC> generic, out Generic<ID, IE, EmptyClass> generic2)
		{
			a = (A)injector.CreateObject(typeof(A), resolver);
			if (inject) injector.Inject(a, resolver);
			resolver.Add<A, A>(a);
			resolver.Add<IA, A>(a);

			b = (B)injector.CreateObject(typeof(B), resolver);
			if (inject) injector.Inject(b, resolver);
			resolver.Add<B, B>(b);
			resolver.Add<IB, B>(b);

			c = (C)injector.CreateObject(typeof(C), resolver);
			if (inject) injector.Inject(c, resolver);
			resolver.Add<C, C>(c);
			resolver.Add<IC, C>(c);

			d = (D)injector.CreateObject(typeof(D), resolver);
			if (inject) injector.Inject(d, resolver);
			resolver.Add<D, D>(d);
			resolver.Add<ID, D>(d);

			e = (E)injector.CreateObject(typeof(E), resolver);
			if (inject) injector.Inject(e, resolver);
			resolver.Add<E, E>(e);
			resolver.Add<IE, E>(e);

			empty = (EmptyClass)injector.CreateObject(typeof(EmptyClass), resolver);
			if (inject) injector.Inject(empty, resolver);
			resolver.Add<EmptyClass, EmptyClass>(empty);

			generic = (Generic<IA, IB, IC>)injector.CreateObject(typeof(Generic<IA, IB, IC>), resolver);
			if (inject) injector.Inject(generic, resolver);

			generic2 = (Generic<ID, IE, EmptyClass>)injector.CreateObject(typeof(Generic<ID, IE, EmptyClass>), resolver);
			if (inject) injector.Inject(generic2, resolver);
		}

		[Test]
		public void TestCreate([ValueSource(nameof(Injectors))] IInjector injector)
		{
			Resolver resolver = new();

			Create(resolver, injector, inject: false, out A a, out B b, out C c, out D d, out E e, out EmptyClass empty, out Generic<IA, IB, IC> generic, out Generic<ID, IE, EmptyClass> generic2);

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
		public void TestCreateAndInject([ValueSource(nameof(Injectors))] IInjector injector)
		{
			Resolver resolver = new();

			Create(resolver, injector, inject: true, out A a, out B b, out C c, out D d, out E e, out EmptyClass empty, out Generic<IA, IB, IC> generic, out Generic<ID, IE, EmptyClass> generic2);

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
		public void TestProtectedSubClassInject([ValueSource(nameof(Injectors))] IInjector injector)
		{
			Resolver resolver = new();

			Create(resolver, injector, inject: true, out A a, out B b, out C c, out D d, out E e, out EmptyClass empty, out Generic<IA, IB, IC> generic, out Generic<ID, IE, EmptyClass> generic2);

			{
				ProtectedSubClassInject<IA> subClass = new();
				injector.Inject(subClass, resolver);
				Assert.AreEqual(a, subClass.Value);
			}

			{
				BaseClass<IA> subClassCastToBase = new ProtectedSubClassInject<IA>();
				injector.Inject(subClassCastToBase, resolver);
				Assert.AreEqual(a, subClassCastToBase.Value);
			}
		}
	}
}
