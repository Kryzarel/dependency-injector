using System;
using NUnit.Framework;

namespace Kryz.DI.Tests
{
	public class ContainerTests
	{
		private enum RegisterType { Singleton, Scoped, Transient }

		private static void GetContainerWithChildren(out Container root, out Container child1, out Container child2, out Container child1_child1, out Container child1_child2, out Container child2_child1, out Container child2_child2)
		{
			root = new Container();

			child1 = root.CreateChild();
			child2 = root.CreateChild();

			child1_child1 = child1.CreateChild();
			child1_child2 = child1.CreateChild();

			child2_child1 = child2.CreateChild();
			child2_child2 = child2.CreateChild();
		}

		[Test]
		public void TestRoot()
		{
			GetContainerWithChildren(out Container root, out Container child1, out Container child2, out Container child1_child1, out Container child1_child2, out Container child2_child1, out Container child2_child2);

			Assert.AreEqual(root, root.Root);

			Assert.AreEqual(root, child1.Root);
			Assert.AreEqual(root, child2.Root);

			Assert.AreEqual(root, child1_child1.Root);
			Assert.AreEqual(root, child1_child2.Root);

			Assert.AreEqual(root, child2_child1.Root);
			Assert.AreEqual(root, child2_child2.Root);
		}

		[Test]
		public void TestParent()
		{
			GetContainerWithChildren(out Container root, out Container child1, out Container child2, out Container child1_child1, out Container child1_child2, out Container child2_child1, out Container child2_child2);

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
			GetContainerWithChildren(out Container root, out Container child1, out Container child2, out Container child1_child1, out Container child1_child2, out Container child2_child1, out Container child2_child2);

			Assert.AreEqual(root.Children[0], child1);
			Assert.AreEqual(root.Children[1], child2);

			Assert.AreEqual(child1.Children[0], child1_child1);
			Assert.AreEqual(child1.Children[1], child1_child2);

			Assert.AreEqual(child2.Children[0], child2_child1);
			Assert.AreEqual(child2.Children[1], child2_child2);
		}

		private static Container Add<TBase, TDerived>(Container container, RegisterType registerType) where TDerived : TBase
		{
			return registerType switch
			{
				RegisterType.Singleton => container.AddSingleton<TBase, TDerived>(),
				RegisterType.Scoped => container.AddScoped<TBase, TDerived>(),
				RegisterType.Transient => container.AddTransient<TBase, TDerived>(),
				_ => throw new NotImplementedException(),
			};
		}

		private static Container SetupContainer(Container container, RegisterType registerType)
		{
			Add<Empty, Empty>(container, registerType);
			Add<IA, A>(container, registerType);
			Add<IB, B>(container, registerType);
			Add<IC, C>(container, registerType);
			Add<ID, D>(container, registerType);
			Add<IE, E>(container, registerType);
			Add<IGeneric<IA, IB, IC>, Generic<IA, IB, IC>>(container, registerType);
			Add<IGeneric<ID, IE, Empty>, Generic<ID, IE, Empty>>(container, registerType);
			Add<ICircular1, Circular1>(container, registerType);
			Add<ICircular2, Circular2>(container, registerType);
			Add<ICircular1NoInject, Circular1NoInject>(container, registerType);
			Add<ICircular2NoInject, Circular2NoInject>(container, registerType);
			return container;
		}

		private static void HasRegistrations(Container container)
		{
			Assert.AreEqual(typeof(Empty), container.GetType<Empty>());
			Assert.AreEqual(typeof(A), container.GetType<IA>());
			Assert.AreEqual(typeof(B), container.GetType<IB>());
			Assert.AreEqual(typeof(C), container.GetType<IC>());
			Assert.AreEqual(typeof(D), container.GetType<ID>());
			Assert.AreEqual(typeof(E), container.GetType<IE>());
			Assert.AreEqual(typeof(Generic<IA, IB, IC>), container.GetType<IGeneric<IA, IB, IC>>());
			Assert.AreEqual(typeof(Generic<ID, IE, Empty>), container.GetType<IGeneric<ID, IE, Empty>>());
			Assert.AreEqual(typeof(Circular1), container.GetType<ICircular1>());
			Assert.AreEqual(typeof(Circular2), container.GetType<ICircular2>());
			Assert.AreEqual(typeof(Circular1NoInject), container.GetType<ICircular1NoInject>());
			Assert.AreEqual(typeof(Circular2NoInject), container.GetType<ICircular2NoInject>());

			Assert.Throws<InjectionException>(() => container.GetObject<A>());
		}

		private static void DoesNotHaveRegistrations(Container container)
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

		private static void HasObjects(Container container)
		{
			Assert.IsTrue(container.GetObject<IA>() is A);
			Assert.IsTrue(container.GetObject<IB>() is B);
			Assert.IsTrue(container.GetObject<IC>() is C);
			Assert.IsTrue(container.GetObject<ID>() is D);
			Assert.IsTrue(container.GetObject<IE>() is E);
			Assert.IsTrue(container.GetObject<IGeneric<IA, IB, IC>>() is Generic<IA, IB, IC>);
			Assert.IsTrue(container.GetObject<IGeneric<ID, IE, Empty>>() is Generic<ID, IE, Empty>);

			Assert.Throws<CircularDependencyException>(() => container.GetObject<ICircular1>());
			Assert.Throws<CircularDependencyException>(() => container.GetObject<ICircular2>());
			Assert.Throws<CircularDependencyException>(() => container.GetObject<ICircular1NoInject>());
			Assert.Throws<CircularDependencyException>(() => container.GetObject<ICircular2NoInject>());
		}

		private static void DoesNotHaveObjects(Container container)
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

		private static void TestObjectEquality(Container container, bool areEqual)
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
		public void TestSingleton()
		{
			Container root = new();

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			SetupContainer(root, RegisterType.Singleton);

			HasRegistrations(root);
			HasObjects(root);
			TestObjectEquality(root, areEqual: true);
		}

		[Test]
		public void TestScoped()
		{
			Container root = new();

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			SetupContainer(root, RegisterType.Scoped);

			HasRegistrations(root);
			HasObjects(root);
			TestObjectEquality(root, areEqual: true);
		}

		[Test]
		public void TestTransient()
		{
			Container root = new();

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			SetupContainer(root, RegisterType.Transient);

			HasRegistrations(root);
			HasObjects(root);
			TestObjectEquality(root, areEqual: false);
		}

		[Test]
		public void TestSingletonChild()
		{
			Container root = new();
			Container child = root.CreateChild();

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			DoesNotHaveRegistrations(child);
			DoesNotHaveObjects(child);

			SetupContainer(child, RegisterType.Singleton);

			HasRegistrations(root);
			HasObjects(root);
			TestObjectEquality(root, areEqual: true);

			HasRegistrations(child);
			HasObjects(child);
			TestObjectEquality(child, areEqual: true);
		}

		[Test]
		public void TestScopedChild()
		{
			Container root = new();
			Container child = root.CreateChild();

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			DoesNotHaveRegistrations(child);
			DoesNotHaveObjects(child);

			SetupContainer(child, RegisterType.Scoped);

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			HasRegistrations(child);
			HasObjects(child);
			TestObjectEquality(child, areEqual: true);
		}

		[Test]
		public void TestTransientChild()
		{
			Container root = new();
			Container child = root.CreateChild();

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			DoesNotHaveRegistrations(child);
			DoesNotHaveObjects(child);

			SetupContainer(child, RegisterType.Transient);

			DoesNotHaveRegistrations(root);
			DoesNotHaveObjects(root);

			HasRegistrations(child);
			HasObjects(child);
			TestObjectEquality(child, areEqual: false);
		}
	}
}