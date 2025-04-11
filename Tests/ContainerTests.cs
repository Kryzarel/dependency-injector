using System;
using System.Reflection;
using Kryz.DI.Exceptions;
using NUnit.Framework;

namespace Kryz.DI.Tests
{
	public class ContainerTests
	{
		/// <summary>
		/// Get the method info for <see cref="ITypeResolver.GetType{T}()"/>
		/// </summary>
		private static readonly MethodInfo getTypeMethod = typeof(ITypeResolver).GetMethod(nameof(ITypeResolver.GetType), 1, Type.EmptyTypes);

		/// <summary>
		/// Get the method info for <see cref="ITypeResolver.TryGetType{T}(out Type)"/>
		/// </summary>
		private static readonly MethodInfo tryGetTypeMethod = typeof(ITypeResolver).GetMethod(nameof(ITypeResolver.TryGetType), 1, new Type[] { typeof(Type).MakeByRefType() });

		/// <summary>
		/// Get the method info for <see cref="IObjectResolver.GetObject{T}()"/>
		/// </summary>
		private static readonly MethodInfo getObjectMethod = typeof(IObjectResolver).GetMethod(nameof(IObjectResolver.GetObject), 1, Type.EmptyTypes);

		/// <summary>
		/// Get the method info for <see cref="IObjectResolver.TryGetObject{T}(out T)"/>
		/// </summary>
		private static readonly MethodInfo tryGetObjectMethod = typeof(IObjectResolver).GetMethod(nameof(IObjectResolver.TryGetObject), 1, new Type[] { Type.MakeGenericMethodParameter(0).MakeByRefType() });

		/// <summary>
		/// Get the method info for <see cref="Builder.Register{TBase, TDerived}(Lifetime)"/>
		/// </summary>
		private static readonly MethodInfo registerMethod = typeof(Builder).GetMethod(nameof(Builder.Register), 2, new Type[] { typeof(Lifetime) });

		public static readonly (Type, Type)[] SafeTypes = new (Type, Type)[] {
			(typeof(EmptyClass), typeof(EmptyClass)),
			(typeof(IA), typeof(A)),
			(typeof(IB), typeof(B)),
			(typeof(IC), typeof(C)),
			(typeof(ID), typeof(D)),
			(typeof(IE), typeof(E)),
			(typeof(IGeneric<IA, IB, IC>), typeof(Generic<IA, IB, IC>)),
			(typeof(IGeneric<ID, IE, EmptyClass>), typeof(Generic<ID, IE, EmptyClass>)),
		};

		public static readonly (Type, Type)[] CircularDependencyTypes = new (Type, Type)[] {
			(typeof(ICircular1DependsOn2), typeof(Circular1)),
			(typeof(ICircular2DependsOn3), typeof(Circular2)),
			(typeof(ICircular3DependsOn1), typeof(Circular3)),
			(typeof(IA), typeof(ACircularDependsOnE)),
			(typeof(IB), typeof(B)),
			(typeof(IC), typeof(C)),
			(typeof(ID), typeof(D)),
			(typeof(IE), typeof(E)),
		};

		public static readonly Lifetime[] Lifetimes = (Lifetime[])Enum.GetValues(typeof(Lifetime));

		[Test]
		public void Container_WithChildScopes_ParentsMatch()
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
		public void Container_WithChildScopes_ChildrenMatch()
		{
			GetContainerWithChildren(out IContainer root, out IContainer child1, out IContainer child2, out IContainer child1_child1, out IContainer child1_child2, out IContainer child2_child1, out IContainer child2_child2);

			Assert.AreEqual(root.ChildScopes[0], child1);
			Assert.AreEqual(root.ChildScopes[1], child2);

			Assert.AreEqual(child1.ChildScopes[0], child1_child1);
			Assert.AreEqual(child1.ChildScopes[1], child1_child2);

			Assert.AreEqual(child2.ChildScopes[0], child2_child1);
			Assert.AreEqual(child2.ChildScopes[1], child2_child2);
		}

		[Test]
		// Given, When, Then
		public void Container_Empty_TryGetType_ReturnsFalse()
		{
			// Arrange, Act
			Builder builder = new();
			IContainer container = builder.Build();

			// Assert
			foreach ((Type, Type) types in SafeTypes)
			{
				Assert.IsFalse(container.TryGetType(types.Item1, out _));

				// Test generic method as well
				MethodInfo methodInfo = tryGetTypeMethod;
				MethodInfo methodInfoGeneric = methodInfo.MakeGenericMethod(types.Item1);
				Assert.IsFalse((bool)methodInfoGeneric.Invoke(container, new object[] { null! }));
			}
		}

		[Test]
		// Given, When, Then
		public void Container_Empty_TryGetObject_ReturnsFalse()
		{
			// Arrange, Act
			Builder builder = new();
			IContainer container = builder.Build();

			// Assert
			foreach ((Type, Type) types in SafeTypes)
			{
				Assert.IsFalse(container.TryGetObject(types.Item1, out _));

				// Test generic method as well
				MethodInfo methodInfo = tryGetObjectMethod;
				MethodInfo methodInfoGeneric = methodInfo.MakeGenericMethod(types.Item1);
				Assert.IsFalse((bool)methodInfoGeneric.Invoke(container, new object[] { null! }));
			}
		}

		[Test]
		// Given, When, Then
		public void Container_Empty_GetType_ThrowsException()
		{
			// Arrange, Act
			Builder builder = new();
			IContainer container = builder.Build();

			// Assert
			foreach ((Type, Type) types in SafeTypes)
			{
				Assert.Throws<InjectionException>(() => container.GetType(types.Item1));

				// Test generic method as well
				MethodInfo methodInfo = getTypeMethod;
				MethodInfo methodInfoGeneric = methodInfo.MakeGenericMethod(types.Item1);
				Assert.Throws<InjectionException>(() => InvokeThrowInnerException(methodInfoGeneric, container, Array.Empty<object>()));
			}
		}

		[Test]
		// Given, When, Then
		public void Container_Empty_GetObject_ThrowsException()
		{
			// Arrange, Act
			Builder builder = new();
			IContainer container = builder.Build();

			// Assert
			foreach ((Type, Type) types in SafeTypes)
			{
				Assert.Throws<InjectionException>(() => container.GetObject(types.Item1));

				// Test generic method as well
				MethodInfo methodInfo = getObjectMethod;
				MethodInfo methodInfoGeneric = methodInfo.MakeGenericMethod(types.Item1);
				Assert.Throws<InjectionException>(() => InvokeThrowInnerException(methodInfoGeneric, container, Array.Empty<object>()));
			}
		}

		[Test]
		// Given, When, Then
		public void Container_SafeRegistrations_GetType_MatchesConcreteType([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
		{
			// Arrange, Act
			Builder builder = new();
			Register(builder, SafeTypes, lifetime);
			IContainer container = builder.Build();

			// Assert
			foreach ((Type, Type) types in SafeTypes)
			{
				Assert.AreEqual(types.Item2, container.GetType(types.Item1));

				// Test generic method as well
				MethodInfo methodInfo = getTypeMethod;
				MethodInfo methodInfoGeneric = methodInfo.MakeGenericMethod(types.Item1);
				Assert.AreEqual(types.Item2, methodInfoGeneric.Invoke(container, Array.Empty<object>()));
			}
		}

		[Test]
		// Given, When, Then
		public void Container_SafeRegistrations_TryGetObject_ReturnsTrue([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
		{
			// Arrange, Act
			Builder builder = new();
			Register(builder, SafeTypes, lifetime);
			IContainer container = builder.Build();

			// Assert
			foreach ((Type, Type) types in SafeTypes)
			{
				Assert.IsTrue(container.TryGetObject(types.Item1, out _));

				// Test generic method as well
				MethodInfo methodInfo = tryGetObjectMethod;
				MethodInfo methodInfoGeneric = methodInfo.MakeGenericMethod(types.Item1);
				Assert.IsTrue((bool)methodInfoGeneric.Invoke(container, new object[] { null! }));
			}
		}

		[Test]
		// Given, When, Then
		public void Container_SafeRegistrations_GetObject_ObjectsMatchLifetime([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
		{
			// Arrange, Act
			Builder builder = new();
			Register(builder, SafeTypes, lifetime);
			IContainer container = builder.Build();

			Action<object, object> assert = lifetime is Lifetime.Singleton or Lifetime.Scoped ? Assert.AreSame : Assert.AreNotSame;

			// Assert
			foreach ((Type, Type) types in SafeTypes)
			{
				assert(container.GetObject(types.Item1), container.GetObject(types.Item1));

				// Test generic method as well
				MethodInfo methodInfo = getObjectMethod;
				MethodInfo methodInfoGeneric = methodInfo.MakeGenericMethod(types.Item1);
				assert(methodInfoGeneric.Invoke(container, Array.Empty<object>()), methodInfoGeneric.Invoke(container, Array.Empty<object>()));
			}
		}

		[Test]
		// Given, When, Then
		public void Container_WithScopes_GetObject_ObjectsMatchParent([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
		{
			// Arrange, Act
			Builder builder = new();
			Register(builder, SafeTypes, lifetime);
			IContainer container = builder.Build();
			IContainer child1 = container.CreateScope();
			IContainer child2 = container.CreateScope();

			Action<object, object> assert = lifetime is Lifetime.Singleton ? Assert.AreSame : Assert.AreNotSame;

			// Assert
			foreach ((Type, Type) types in SafeTypes)
			{
				assert(container.GetObject(types.Item1), child1.GetObject(types.Item1));
				assert(container.GetObject(types.Item1), child2.GetObject(types.Item1));
				assert(child1.GetObject(types.Item1), child2.GetObject(types.Item1));

				// Test generic method as well
				MethodInfo methodInfo = getObjectMethod;
				MethodInfo methodInfoGeneric = methodInfo.MakeGenericMethod(types.Item1);
				assert(methodInfoGeneric.Invoke(container, Array.Empty<object>()), methodInfoGeneric.Invoke(child1, Array.Empty<object>()));
				assert(methodInfoGeneric.Invoke(container, Array.Empty<object>()), methodInfoGeneric.Invoke(child2, Array.Empty<object>()));
				assert(methodInfoGeneric.Invoke(child1, Array.Empty<object>()), methodInfoGeneric.Invoke(child2, Array.Empty<object>()));
			}
		}

		[Test]
		// Given, When, Then
		public void Builder_MissingDependencies_Build_ThrowsException([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
		{
			// Arrange, Act
			Builder builder = new();

			for (int i = 0; i < SafeTypes.Length; i += 2) // Register only every other type
			{
				Register(builder, SafeTypes[i], lifetime);
			}

			// Assert
			Assert.Throws<MissingDependencyException>(() => builder.Build());
		}

		[Test]
		// Given, When, Then
		public void Builder_CircularDependencies_Build_ThrowsException([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
		{
			// Arrange, Act
			Builder builder = new();
			Register(builder, CircularDependencyTypes, lifetime);

			// Assert
			Assert.Throws<CircularDependencyException>(() => builder.Build());
		}

		[Test]
		public void TestInject()
		{
			IContainer root = new Builder().Build();
			IContainer child = root.CreateScope(builder => Register(builder, SafeTypes, Lifetime.Singleton));

			{
				Generic<IA, IB, IC> generic = new();
				Assert.Throws<InjectionException>(() => root.Inject(generic));

				child.Inject(generic);
				Assert.AreEqual(child.GetObject<IA>(), generic.One);
				Assert.AreEqual(child.GetObject<IB>(), generic.Two);
				Assert.AreEqual(child.GetObject<IC>(), generic.Three);
			}
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
			Assert.AreEqual(startingCount + 3, InstanceCounter.Count);
			Assert.IsTrue(child2.TryGetObject<InstanceCounter>(out _));
			Assert.AreEqual(startingCount + 4, InstanceCounter.Count);
			Assert.IsTrue(child2.TryGetObject<InstanceCounter>(out _));
			Assert.AreEqual(startingCount + 5, InstanceCounter.Count);
		}

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

		// TODO: Possibly change to private after deleting ContainerTestHelper
		public static void Register(Builder builder, (Type, Type)[] registerTypes, Lifetime lifetime)
		{
			foreach ((Type, Type) types in registerTypes)
			{
				Register(builder, types, lifetime);
			}
		}

		private static void Register(Builder builder, (Type, Type) types, Lifetime lifetime)
		{
			MethodInfo registerMethodGeneric = registerMethod.MakeGenericMethod(types.Item1, types.Item2);
			registerMethodGeneric.Invoke(builder, new object[] { lifetime });
		}

		private static void InvokeThrowInnerException(MethodInfo info, object obj, object[] parameters)
		{
			try
			{
				info.Invoke(obj, parameters);
			}
			catch (TargetInvocationException exception)
			{
				throw exception.InnerException;
			}
		}
	}
}