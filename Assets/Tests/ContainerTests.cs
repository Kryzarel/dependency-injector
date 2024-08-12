using System;
using NUnit.Framework;

namespace Kryz.DI.Tests
{
	public class ContainerTests
	{
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

		private static Container GetSingletonContainer()
		{
			return new Container()
				.AddSingleton<Empty, Empty>()
				.AddSingleton<IA, A>()
				.AddSingleton<IB, B>()
				.AddSingleton<IC, C>()
				.AddSingleton<ID, D>()
				.AddSingleton<IE, E>()
				.AddSingleton<IGeneric<IA, IB, IC>, Generic<IA, IB, IC>>()
				.AddSingleton<IGeneric<ID, IE, Empty>, Generic<ID, IE, Empty>>()
				.AddSingleton<ICircular1, Circular1>()
				.AddSingleton<ICircular2, Circular2>()
				.AddSingleton<ICircular1NoInject, Circular1NoInject>()
				.AddSingleton<ICircular2NoInject, Circular2NoInject>();
		}

		private static Container GetScopedContainer()
		{
			return new Container()
				.AddScoped<Empty, Empty>()
				.AddScoped<IA, A>()
				.AddScoped<IB, B>()
				.AddScoped<IC, C>()
				.AddScoped<ID, D>()
				.AddScoped<IE, E>()
				.AddScoped<IGeneric<IA, IB, IC>, Generic<IA, IB, IC>>()
				.AddScoped<IGeneric<ID, IE, Empty>, Generic<ID, IE, Empty>>()
				.AddScoped<ICircular1, Circular1>()
				.AddScoped<ICircular2, Circular2>()
				.AddScoped<ICircular1NoInject, Circular1NoInject>()
				.AddScoped<ICircular2NoInject, Circular2NoInject>();
		}

		private static void TestRegistrations(Container container)
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

		private static void TestObjects(Container container)
		{
			Assert.IsTrue(container.GetObject<IA>() is A);
			Assert.IsTrue(container.GetObject<IB>() is B);
			Assert.IsTrue(container.GetObject<IC>() is C);
			Assert.IsTrue(container.GetObject<ID>() is D);
			Assert.IsTrue(container.GetObject<IE>() is E);
			Assert.IsTrue(container.GetObject<IGeneric<IA, IB, IC>>() is Generic<IA, IB, IC>);
			Assert.IsTrue(container.GetObject<IGeneric<ID, IE, Empty>>() is Generic<ID, IE, Empty>);

			Assert.AreEqual(container.GetObject<IA>(), container.GetObject<IC>().A);
			Assert.AreEqual(container.GetObject<IB>(), container.GetObject<ID>().B);

			E e = (E)container.GetObject<IE>();
			Assert.AreEqual(container.GetObject<IA>(), e.A);
			Assert.AreEqual(container.GetObject<IB>(), e.B);
			Assert.AreEqual(container.GetObject<IC>(), e.C);
			Assert.AreEqual(container.GetObject<ID>(), e.D);

			Assert.Throws<InjectionException>(() => container.GetObject<ICircular1>());
			Assert.Throws<InjectionException>(() => container.GetObject<ICircular2>());
			Assert.Throws<InjectionException>(() => container.GetObject<ICircular1NoInject>());
			Assert.Throws<InjectionException>(() => container.GetObject<ICircular2NoInject>());
		}

		[Test]
		public void TestSingletonRegistrations()
		{
			TestRegistrations(GetSingletonContainer());
		}

		[Test]
		public void TestSingletonObjects()
		{
			TestObjects(GetSingletonContainer());
		}

		[Test]
		public void TestScopedRegistrations()
		{
			TestRegistrations(GetScopedContainer());
		}

		[Test]
		public void TestScopedObjects()
		{
			TestObjects(GetScopedContainer());
		}
	}
}