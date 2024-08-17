using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Kryz.DI.Tests
{
	public class ReflectionCacheTests
	{
		[Test]
		public void TestCache()
		{
			ReflectionCache reflectionCache = new();
			Assert.AreEqual(reflectionCache.Get(typeof(IA)), reflectionCache.Get(typeof(IA)));
			Assert.AreEqual(reflectionCache.Get(typeof(IB)), reflectionCache.Get(typeof(IB)));
			Assert.AreEqual(reflectionCache.Get(typeof(IC)), reflectionCache.Get(typeof(IC)));
			Assert.AreEqual(reflectionCache.Get(typeof(ID)), reflectionCache.Get(typeof(ID)));
			Assert.AreEqual(reflectionCache.Get(typeof(A)), reflectionCache.Get(typeof(A)));
			Assert.AreEqual(reflectionCache.Get(typeof(B)), reflectionCache.Get(typeof(B)));
			Assert.AreEqual(reflectionCache.Get(typeof(C)), reflectionCache.Get(typeof(C)));
			Assert.AreEqual(reflectionCache.Get(typeof(D)), reflectionCache.Get(typeof(D)));
			Assert.AreEqual(reflectionCache.Get(typeof(Empty)), reflectionCache.Get(typeof(Empty)));
			Assert.AreEqual(reflectionCache.Get(typeof(EmptyStruct)), reflectionCache.Get(typeof(EmptyStruct)));

			ReflectionCache reflectionCache2 = new();
			Assert.AreNotEqual(reflectionCache2.Get(typeof(IA)), reflectionCache.Get(typeof(IA)));
			Assert.AreNotEqual(reflectionCache2.Get(typeof(IB)), reflectionCache.Get(typeof(IB)));
			Assert.AreNotEqual(reflectionCache2.Get(typeof(IC)), reflectionCache.Get(typeof(IC)));
			Assert.AreNotEqual(reflectionCache2.Get(typeof(ID)), reflectionCache.Get(typeof(ID)));
			Assert.AreNotEqual(reflectionCache2.Get(typeof(A)), reflectionCache.Get(typeof(A)));
			Assert.AreNotEqual(reflectionCache2.Get(typeof(B)), reflectionCache.Get(typeof(B)));
			Assert.AreNotEqual(reflectionCache2.Get(typeof(C)), reflectionCache.Get(typeof(C)));
			Assert.AreNotEqual(reflectionCache2.Get(typeof(D)), reflectionCache.Get(typeof(D)));
			Assert.AreNotEqual(reflectionCache2.Get(typeof(Empty)), reflectionCache.Get(typeof(Empty)));
			Assert.AreNotEqual(reflectionCache2.Get(typeof(EmptyStruct)), reflectionCache.Get(typeof(EmptyStruct)));
		}

		[Test]
		public void TestCacheSpeed()
		{
			ReflectionCache reflectionCache = new();

			Stopwatch stopwatch = new();

			stopwatch.Restart();
			reflectionCache.Get(typeof(A));
			stopwatch.Stop();
			long first = stopwatch.ElapsedTicks;

			stopwatch.Restart();
			reflectionCache.Get(typeof(A));
			stopwatch.Stop();
			long second = stopwatch.ElapsedTicks;

			Assert.Less(second, first);
		}

		[Test]
		public void TestInfoEmpty()
		{
			ReflectionCache.InjectionInfo info = TestTypeInfo<Empty>(
				hasConstructor: true,
				numConstructorParams: 0,
				numFields: 0,
				numProperties: 0,
				numMethods: 0);

			Assert.AreEqual(typeof(Empty).GetConstructors()[0], info.Constructor);
		}

		[Test]
		public void TestInfoEmptyStruct()
		{
			TestTypeInfo<EmptyStruct>(
				hasConstructor: false,
				numConstructorParams: 0,
				numFields: 0,
				numProperties: 0,
				numMethods: 0);
		}

		[Test]
		public void TestInfoA()
		{
			ReflectionCache.InjectionInfo info = TestTypeInfo<A>(
				hasConstructor: true,
				numConstructorParams: 0,
				numFields: 0,
				numProperties: 0,
				numMethods: 0);

			Assert.AreEqual(typeof(A).GetConstructors()[0], info.Constructor);
		}

		[Test]
		public void TestInfoB()
		{
			ReflectionCache.InjectionInfo info = TestTypeInfo<B>(
				hasConstructor: true,
				numConstructorParams: 1,
				numFields: 0,
				numProperties: 0,
				numMethods: 0);

			Assert.AreEqual(typeof(B).GetConstructors()[0], info.Constructor);
			Assert.AreEqual(typeof(IA), info.ConstructorParams[0]);
		}

		[Test]
		public void TestInfoC()
		{
			ReflectionCache.InjectionInfo info = TestTypeInfo<C>(
				hasConstructor: true,
				numConstructorParams: 2,
				numFields: 0,
				numProperties: 0,
				numMethods: 0);

			Assert.AreEqual(typeof(C).GetConstructors().Single(x => x.IsDefined(typeof(InjectAttribute))), info.Constructor);
			Assert.AreEqual(typeof(IA), info.ConstructorParams[0]);
			Assert.AreEqual(typeof(IB), info.ConstructorParams[1]);
		}

		[Test]
		public void TestInfoD()
		{
			ReflectionCache.InjectionInfo info = TestTypeInfo<D>(
				hasConstructor: true,
				numConstructorParams: 0,
				numFields: 1,
				numProperties: 1,
				numMethods: 1);

			Assert.AreEqual(typeof(D).GetConstructors()[0], info.Constructor);
			Assert.AreEqual(typeof(D).GetField(nameof(D.A)), info.Fields[0]);
			Assert.AreEqual(typeof(D).GetProperty(nameof(D.B)), info.Properties[0]);
			Assert.AreEqual(typeof(D).GetMethod(nameof(D.InjectC)), info.Methods[0]);
		}

		[Test]
		public void TestInfoE()
		{
			ReflectionCache.InjectionInfo info = TestTypeInfo<E>(
				hasConstructor: true,
				numConstructorParams: 0,
				numFields: 0,
				numProperties: 0,
				numMethods: 2);

			Assert.AreEqual(typeof(E).GetConstructors()[0], info.Constructor);
			Assert.IsTrue(info.Methods.Contains(typeof(E).GetMethod(nameof(E.InjectA))));
			Assert.IsTrue(info.Methods.Contains(typeof(E).GetMethod(nameof(E.InjectBCD))));
		}

		[Test]
		public void TestInfoGeneric()
		{
			ReflectionCache.InjectionInfo info = TestTypeInfo<Generic<IA, IB, IC>>(
				hasConstructor: true,
				numConstructorParams: 0,
				numFields: 0,
				numProperties: 0,
				numMethods: 1);

			Assert.AreEqual(typeof(Generic<IA, IB, IC>).GetConstructors()[0], info.Constructor);
			Assert.IsTrue(info.Methods.Contains(typeof(Generic<IA, IB, IC>).GetMethod(nameof(Generic<IA, IB, IC>.Inject123))));
		}

		[Test]
		public void TestInfoIA()
		{
			TestTypeInfo<IA>(
				hasConstructor: false,
				numConstructorParams: 0,
				numFields: 0,
				numProperties: 0,
				numMethods: 0);
		}

		[Test]
		public void TestInfoIB()
		{
			TestTypeInfo<IB>(
				hasConstructor: false,
				numConstructorParams: 0,
				numFields: 0,
				numProperties: 0,
				numMethods: 0);
		}

		[Test]
		public void TestInfoIC()
		{
			ReflectionCache.InjectionInfo info = TestTypeInfo<IC>(
				hasConstructor: false,
				numConstructorParams: 0,
				numFields: 0,
				numProperties: 1,
				numMethods: 0);

			Assert.AreEqual(typeof(IC).GetProperty(nameof(IC.A)), info.Properties[0]);
		}

		[Test]
		public void TestInfoID()
		{
			ReflectionCache.InjectionInfo info = TestTypeInfo<ID>(
				hasConstructor: false,
				numConstructorParams: 0,
				numFields: 0,
				numProperties: 1,
				numMethods: 1);

			Assert.AreEqual(typeof(ID).GetProperty(nameof(ID.B)), info.Properties[0]);
			Assert.AreEqual(typeof(ID).GetMethod(nameof(ID.InjectC)), info.Methods[0]);
		}

		[Test]
		public void TestInfoIE()
		{
			ReflectionCache.InjectionInfo info = TestTypeInfo<IE>(
				hasConstructor: false,
				numConstructorParams: 0,
				numFields: 0,
				numProperties: 0,
				numMethods: 2);

			Assert.IsTrue(info.Methods.Contains(typeof(IE).GetMethod(nameof(IE.InjectA))));
			Assert.IsTrue(info.Methods.Contains(typeof(IE).GetMethod(nameof(IE.InjectBCD))));
		}

		[Test]
		public void TestInfoIGeneric()
		{
			ReflectionCache.InjectionInfo info = TestTypeInfo<IGeneric<IA, IB, IC>>(
				hasConstructor: false,
				numConstructorParams: 0,
				numFields: 0,
				numProperties: 0,
				numMethods: 1);

			Assert.IsTrue(info.Methods.Contains(typeof(IGeneric<IA, IB, IC>).GetMethod(nameof(IGeneric<IA, IB, IC>.Inject123))));
		}

		[Test]
		public void TestInfoProtectedSubClassInject()
		{
			ReflectionCache.InjectionInfo info = TestTypeInfo<ProtectedSubClassInject<IA>>(
				hasConstructor: true,
				numConstructorParams: 0,
				numFields: 0,
				numProperties: 3,
				numMethods: 1);

			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			Assert.IsTrue(info.Methods.Contains(typeof(ProtectedSubClassInject<IA>).GetMethod("Inject", flags)));
		}

		private static ReflectionCache.InjectionInfo TestTypeInfo<T>(bool hasConstructor, int numConstructorParams, int numFields, int numProperties, int numMethods)
		{
			Type type = typeof(T);
			ReflectionCache reflectionCache = new();
			ReflectionCache.InjectionInfo info = reflectionCache.Get(type);

			if (hasConstructor)
			{
				Assert.AreNotEqual(null, info.Constructor, "Constructor");
			}
			else
			{
				Assert.AreEqual(null, info.Constructor, "Constructor");
			}

			Assert.AreEqual(numConstructorParams, info.ConstructorParams.Count, "Constructor Params");
			if (numConstructorParams == 0)
			{
				Assert.AreEqual(Array.Empty<Type>(), info.ConstructorParams, "Constructor Params");
			}

			Assert.AreEqual(numFields, info.Fields.Count, "Fields");
			if (numFields == 0)
			{
				Assert.AreEqual(Array.Empty<FieldInfo>(), info.Fields, "Fields");
			}

			Assert.AreEqual(numProperties, info.Properties.Count, "Properties");
			if (numProperties == 0)
			{
				Assert.AreEqual(Array.Empty<PropertyInfo>(), info.Properties, "Properties");
			}

			Assert.AreEqual(numMethods, info.Methods.Count, "Methods");
			if (numMethods == 0)
			{
				Assert.AreEqual(Array.Empty<MethodInfo>(), info.Methods, "Methods");
			}

			return info;
		}
	}
}
