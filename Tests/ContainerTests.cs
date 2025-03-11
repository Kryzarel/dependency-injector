using System;
using Kryz.DI.Exceptions;
using NUnit.Framework;
using static Kryz.DI.Tests.ContainerTestHelper;

namespace Kryz.DI.Tests
{
	public class ContainerTests
	{
		private static void GetContainerWithChildren(out IContainer root, out IContainer child1, out IContainer child2, out IContainer child1_child1, out IContainer child1_child2, out IContainer child2_child1, out IContainer child2_child2)
		{
			root = new Builder().Build();

			child1 = root.CreateScope();
			child2 = root.CreateScope();

			child1_child1 = child1.CreateScope();
			child1_child2 = child1.CreateScope();

			child2_child1 = child2.CreateScope();
			child2_child2 = child2.CreateScope();
		}

		[Test]
		public void TestParent()
		{
			GetContainerWithChildren(out IContainer root, out IContainer child1, out IContainer child2, out IContainer child1_child1, out IContainer child1_child2, out IContainer child2_child1, out IContainer child2_child2);

			Assert.AreEqual(root, child1.Parent);
			Assert.AreEqual(root, child2.Parent);

			Assert.AreEqual(child1, child1_child1.Parent);
			Assert.AreEqual(child1, child1_child2.Parent);

			Assert.AreEqual(child2, child2_child1.Parent);
			Assert.AreEqual(child2, child2_child2.Parent);
		}

		[Test]
		public void TestChildren()
		{
			GetContainerWithChildren(out IContainer root, out IContainer child1, out IContainer child2, out IContainer child1_child1, out IContainer child1_child2, out IContainer child2_child1, out IContainer child2_child2);

			Assert.AreEqual(root.ChildScopes[0], child1);
			Assert.AreEqual(root.ChildScopes[1], child2);

			Assert.AreEqual(child1.ChildScopes[0], child1_child1);
			Assert.AreEqual(child1.ChildScopes[1], child1_child2);

			Assert.AreEqual(child2.ChildScopes[0], child2_child1);
			Assert.AreEqual(child2.ChildScopes[1], child2_child2);
		}

		private static void HasRegistrations(IContainer container)
		{
			Assert.AreEqual(typeof(Empty), container.GetType<Empty>());
			Assert.AreEqual(typeof(A), container.GetType<IA>());
			Assert.AreEqual(typeof(B), container.GetType<IB>());
			Assert.AreEqual(typeof(C), container.GetType<IC>());
			Assert.AreEqual(typeof(D), container.GetType<ID>());
			Assert.AreEqual(typeof(E), container.GetType<IE>());
			Assert.AreEqual(typeof(Generic<IA, IB, IC>), container.GetType<IGeneric<IA, IB, IC>>());
			Assert.AreEqual(typeof(Generic<ID, IE, Empty>), container.GetType<IGeneric<ID, IE, Empty>>());

			Assert.Throws<InjectionException>(() => container.GetObject<A>());
		}

		private static void DoesNotHaveRegistrations(IContainer container)
		{
			Assert.IsFalse(container.TryGetType<Empty>(out _));
			Assert.IsFalse(container.TryGetType<IA>(out _));
			Assert.IsFalse(container.TryGetType<IB>(out _));
			Assert.IsFalse(container.TryGetType<IC>(out _));
			Assert.IsFalse(container.TryGetType<ID>(out _));
			Assert.IsFalse(container.TryGetType<IE>(out _));
			Assert.IsFalse(container.TryGetType<IGeneric<IA, IB, IC>>(out _));
			Assert.IsFalse(container.TryGetType<IGeneric<ID, IE, Empty>>(out _));
			Assert.IsFalse(container.TryGetType<ICircular1>(out _));
			Assert.IsFalse(container.TryGetType<ICircular2>(out _));
			Assert.IsFalse(container.TryGetType<ICircular1NoInject>(out _));
			Assert.IsFalse(container.TryGetType<ICircular2NoInject>(out _));

			Assert.Throws<InjectionException>(() => container.GetObject<A>());
		}

		private static void HasObjects(IContainer container)
		{
			Assert.IsTrue(container.GetObject<IA>() is A);
			Assert.IsTrue(container.GetObject<IB>() is B);
			Assert.IsTrue(container.GetObject<IC>() is C);
			Assert.IsTrue(container.GetObject<ID>() is D);
			Assert.IsTrue(container.GetObject<IE>() is E);
			Assert.IsTrue(container.GetObject<IGeneric<IA, IB, IC>>() is Generic<IA, IB, IC>);
			Assert.IsTrue(container.GetObject<IGeneric<ID, IE, Empty>>() is Generic<ID, IE, Empty>);
		}

		private static void DoesNotHaveObjects(IContainer container)
		{
			Assert.IsFalse(container.TryGetObject<IA>(out _));
			Assert.IsFalse(container.TryGetObject<IB>(out _));
			Assert.IsFalse(container.TryGetObject<IC>(out _));
			Assert.IsFalse(container.TryGetObject<ID>(out _));
			Assert.IsFalse(container.TryGetObject<IE>(out _));
			Assert.IsFalse(container.TryGetObject<IGeneric<IA, IB, IC>>(out _));
			Assert.IsFalse(container.TryGetObject<IGeneric<ID, IE, Empty>>(out _));

			Assert.Throws<InjectionException>(() => container.GetObject<ICircular1>());
			Assert.Throws<InjectionException>(() => container.GetObject<ICircular2>());
			Assert.Throws<InjectionException>(() => container.GetObject<ICircular1NoInject>());
			Assert.Throws<InjectionException>(() => container.GetObject<ICircular2NoInject>());
		}

		private static void TestObjectEquality(IContainer container, bool areEqual)
		{
			Action<object?, object?> assertEquality = areEqual ? Assert.AreEqual : Assert.AreNotEqual;

			assertEquality(container.GetObject<IA>(), container.GetObject<IA>());
			assertEquality(container.GetObject<IB>(), container.GetObject<IB>());
			assertEquality(container.GetObject<IC>(), container.GetObject<IC>());
			assertEquality(container.GetObject<ID>(), container.GetObject<ID>());
			assertEquality(container.GetObject<IE>(), container.GetObject<IE>());
			assertEquality(container.GetObject<IGeneric<IA, IB, IC>>(), container.GetObject<IGeneric<IA, IB, IC>>());
			assertEquality(container.GetObject<IGeneric<ID, IE, Empty>>(), container.GetObject<IGeneric<ID, IE, Empty>>());

			assertEquality(container.GetObject<IA>(), container.GetObject<IC>().A);
			assertEquality(container.GetObject<IB>(), container.GetObject<ID>().B);

			E e = (E)container.GetObject<IE>();
			assertEquality(container.GetObject<IA>(), e.A);
			assertEquality(container.GetObject<IB>(), e.B);
			assertEquality(container.GetObject<IC>(), e.C);
			assertEquality(container.GetObject<ID>(), e.D);
		}

		[Test]
		public void TestRegistrations()
		{
			IContainer container = SetupContainer(Lifetime.Scoped);

			Assert.AreEqual(typeof(Empty), container.GetType<Empty>());
			Assert.AreEqual(typeof(A), container.GetType<IA>());
			Assert.AreEqual(typeof(B), container.GetType<IB>());
			Assert.AreEqual(typeof(C), container.GetType<IC>());
			Assert.AreEqual(typeof(D), container.GetType<ID>());
			Assert.AreEqual(typeof(E), container.GetType<IE>());
			Assert.AreEqual(typeof(Generic<IA, IB, IC>), container.GetType<IGeneric<IA, IB, IC>>());
			Assert.AreEqual(typeof(Generic<ID, IE, Empty>), container.GetType<IGeneric<ID, IE, Empty>>());

			Assert.Throws<CircularDependencyException>(() => new Builder().Register<ICircular1, Circular1>(Lifetime.Singleton).Build());
			Assert.Throws<CircularDependencyException>(() => new Builder().Register<ICircular2, Circular2>(Lifetime.Singleton).Build());

			Assert.Throws<MissingDependencyException>(() => new Builder().Register<ICircular1NoInject, Circular1NoInject>(Lifetime.Singleton).Build());

			Builder builder = new();
			builder.Register<ICircular1NoInject, Circular1NoInject>(Lifetime.Singleton);
			builder.Register<ICircular2NoInject, Circular2NoInject>(Lifetime.Singleton);
			Assert.Throws<CircularDependencyException>(() => builder.Build());
		}

		[Test]
		public void TestSingleton()
		{
			IContainer empty = new Builder().Build();
			DoesNotHaveRegistrations(empty);
			DoesNotHaveObjects(empty);

			IContainer container = SetupContainer(Lifetime.Singleton);
			HasRegistrations(container);
			HasObjects(container);
			TestObjectEquality(container, areEqual: true);
		}

		[Test]
		public void TestScoped()
		{
			IContainer empty = new Builder().Build();
			DoesNotHaveRegistrations(empty);
			DoesNotHaveObjects(empty);

			IContainer container = SetupContainer(Lifetime.Scoped);
			HasRegistrations(container);
			HasObjects(container);
			TestObjectEquality(container, areEqual: true);
		}

		[Test]
		public void TestTransient()
		{
			IContainer empty = new Builder().Build();
			DoesNotHaveRegistrations(empty);
			DoesNotHaveObjects(empty);

			IContainer container = SetupContainer(Lifetime.Transient);
			HasRegistrations(container);
			HasObjects(container);
			TestObjectEquality(container, areEqual: false);
		}

		[Test]
		public void TestSingletonChild()
		{
			IContainer root = new Builder().Build();

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			IContainer child = root.CreateScope(builder => Register(builder, Lifetime.Singleton));

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			HasRegistrations(child);
			HasObjects(child);
			TestObjectEquality(child, areEqual: true);
		}

		[Test]
		public void TestScopedChild()
		{
			IContainer root = new Builder().Build();

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			IContainer child = root.CreateScope(builder => Register(builder, Lifetime.Scoped));

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			HasRegistrations(child);
			HasObjects(child);
			TestObjectEquality(child, areEqual: true);
		}

		[Test]
		public void TestTransientChild()
		{
			IContainer root = new Builder().Build();

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			IContainer child = root.CreateScope(builder => Register(builder, Lifetime.Transient));

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			HasRegistrations(child);
			HasObjects(child);
			TestObjectEquality(child, areEqual: false);
		}

		[Test]
		public void TestAddObject()
		{
			IContainer empty = new Builder().Build();

			Assert.IsFalse(empty.TryGetType<Empty>(out _));
			Assert.IsFalse(empty.TryGetObject<Empty>(out _));
			Assert.Throws<InjectionException>(() => empty.GetType<Empty>());
			Assert.Throws<InjectionException>(() => empty.GetObject<Empty>());

			Empty emptyObj = new();
			IContainer container = new Builder().Register(emptyObj).Build();

			Assert.IsTrue(container.TryGetType<Empty>(out _));
			Assert.IsTrue(container.TryGetObject<Empty>(out _));
			Assert.AreEqual(typeof(Empty), container.GetType<Empty>());
			Assert.AreEqual(emptyObj, container.GetObject<Empty>());
		}

		[Test]
		public void TestInject()
		{
			IContainer root = new Builder().Build();
			IContainer child = root.CreateScope(builder => Register(builder, Lifetime.Singleton));

			{
				Generic<IA, IB, IC> generic = new();
				Assert.Throws<InjectionException>(() => root.Injector.Inject(generic, root));

				child.Injector.Inject(generic, child);
				Assert.AreEqual(child.GetObject<IA>(), generic.One);
				Assert.AreEqual(child.GetObject<IB>(), generic.Two);
				Assert.AreEqual(child.GetObject<IC>(), generic.Three);
			}
		}

		private class InstanceCounter
		{
			public static int Count;
			public InstanceCounter() { Count++; }
			~InstanceCounter() { Count--; }
		}

		[Test]
		public void TestInstantiate()
		{
			IContainer root = new Builder().Register<InstanceCounter>(Lifetime.Scoped).Build();
			IContainer child = root.CreateScope();
			IContainer child2 = child.CreateScope(builder => builder.Register<InstanceCounter>(Lifetime.Transient));

			int startingCount = InstanceCounter.Count;

			root.TryGetObject<InstanceCounter>(out _);
			Assert.AreEqual(startingCount + 1, InstanceCounter.Count);
			Assert.IsTrue(root.TryGetObject<InstanceCounter>(out _));
			Assert.AreEqual(startingCount + 1, InstanceCounter.Count);

			child.TryGetObject<InstanceCounter>(out _);
			Assert.AreEqual(startingCount + 2, InstanceCounter.Count);
			Assert.IsTrue(child.TryGetObject<InstanceCounter>(out _));
			Assert.AreEqual(startingCount + 2, InstanceCounter.Count);

			child2.TryGetObject<InstanceCounter>(out _);
			Assert.AreEqual(startingCount + 2, InstanceCounter.Count);
			Assert.IsTrue(child2.TryGetObject<InstanceCounter>(out _));
			Assert.AreEqual(startingCount + 3, InstanceCounter.Count);
			Assert.IsTrue(child2.TryGetObject<InstanceCounter>(out _));
			Assert.AreEqual(startingCount + 4, InstanceCounter.Count);
		}
	}
}