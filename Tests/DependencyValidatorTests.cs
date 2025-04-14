using System;
using System.Collections.Generic;
using Kryz.DI.Internal;
using Kryz.DI.Reflection;
using Kryz.Utils;
using NUnit.Framework;

namespace Kryz.DI.Tests
{
	public class DependencyValidatorTests
	{
		[Test]
		// Given, When, Then
		public void CircularDependency_Path_ABCDE()
		{
			// Arrange
			Dictionary<Type, object> objects = new();
			Dictionary<Type, Registration> registrations = new();

			IInjector injector = new ReflectionInjector();
			Container container = new(injector, registrations, objects);

			registrations[typeof(IA)] = new Registration(typeof(ACircularDependsOnE), Lifetime.Singleton);
			registrations[typeof(IB)] = new Registration(typeof(B), Lifetime.Singleton);
			registrations[typeof(IC)] = new Registration(typeof(C), Lifetime.Singleton);
			registrations[typeof(ID)] = new Registration(typeof(D), Lifetime.Singleton);
			registrations[typeof(IE)] = new Registration(typeof(E), Lifetime.Singleton);

			// Act
			DependencyValidator.Data data = DependencyValidator.Validate(container, injector, registrations, objects);

			// Assert
			Assert.IsNull(data.MissingDependencies);

			Assert.IsNotNull(data.CircularDependencies);
			Assert.Greater(data.CircularDependencies!.Count, 0);

			Assert.IsTrue(data.CircularDependencies!.TryGetValue(typeof(IA), out IReadOnlyList<Type>? path));
			Assert.IsTrue(new Type[] { typeof(ACircularDependsOnE), typeof(E), typeof(ACircularDependsOnE) }.ContentEquals(path));

			Assert.IsTrue(data.CircularDependencies!.TryGetValue(typeof(IE), out path));
			Assert.IsTrue(new Type[] { typeof(E), typeof(ACircularDependsOnE), typeof(E) }.ContentEquals(path));
		}

		[Test]
		// Given, When, Then
		public void CircularDependency_Path_Circular1Circular2Circular3()
		{
			// Arrange
			Dictionary<Type, object> objects = new();
			Dictionary<Type, Registration> registrations = new();

			IInjector injector = new ReflectionInjector();
			Container container = new(injector, registrations, objects);

			registrations[typeof(ICircular1DependsOn2)] = new Registration(typeof(Circular1), Lifetime.Singleton);
			registrations[typeof(ICircular2DependsOn3)] = new Registration(typeof(Circular2), Lifetime.Singleton);
			registrations[typeof(ICircular3DependsOn1)] = new Registration(typeof(Circular3), Lifetime.Singleton);

			// Act
			DependencyValidator.Data data = DependencyValidator.Validate(container, injector, registrations, objects);

			// Assert
			Assert.IsNull(data.MissingDependencies);

			Assert.IsNotNull(data.CircularDependencies);
			Assert.Greater(data.CircularDependencies!.Count, 0);

			Assert.IsTrue(data.CircularDependencies.TryGetValue(typeof(ICircular1DependsOn2), out IReadOnlyList<Type>? path));
			Assert.IsTrue(new Type[] { typeof(Circular1), typeof(Circular2), typeof(Circular1) }.ContentEquals(path));

			Assert.IsTrue(data.CircularDependencies.TryGetValue(typeof(ICircular2DependsOn3), out path));
			Assert.IsTrue(new Type[] { typeof(Circular2), typeof(Circular1), typeof(Circular2) }.ContentEquals(path));

			Assert.IsTrue(data.CircularDependencies.TryGetValue(typeof(ICircular3DependsOn1), out path));
			Assert.IsTrue(new Type[] { typeof(Circular3), typeof(Circular1), typeof(Circular2), typeof(Circular1) }.ContentEquals(path));
		}

		[Test]
		// Given, When, Then
		public void MissingDependencies_AB()
		{
			// Arrange
			Dictionary<Type, object> objects = new();
			Dictionary<Type, Registration> registrations = new();

			IInjector injector = new ReflectionInjector();
			Container container = new(injector, registrations, objects);

			// registrations[typeof(IA)] = new Registration(typeof(A), Lifetime.Singleton);
			// registrations[typeof(IB)] = new Registration(typeof(B), Lifetime.Singleton);
			registrations[typeof(IC)] = new Registration(typeof(C), Lifetime.Singleton);
			registrations[typeof(ID)] = new Registration(typeof(D), Lifetime.Singleton);
			registrations[typeof(IE)] = new Registration(typeof(E), Lifetime.Singleton);

			// Act
			DependencyValidator.Data data = DependencyValidator.Validate(container, injector, registrations, objects);

			// Assert
			Assert.IsNull(data.CircularDependencies);

			Assert.IsNotNull(data.MissingDependencies);
			Assert.Greater(data.MissingDependencies!.Count, 0);

			Assert.IsTrue(data.MissingDependencies.TryGetValue(typeof(IC), out IReadOnlyList<Type> missing));
			Assert.IsTrue(new Type[] { typeof(IA), typeof(IB) }.ContentEquals(missing));

			Assert.IsTrue(data.MissingDependencies.TryGetValue(typeof(ID), out missing));
			Assert.IsTrue(new Type[] { typeof(IA), typeof(IB) }.ContentEquals(missing));

			Assert.IsTrue(data.MissingDependencies.TryGetValue(typeof(IE), out missing));
			Assert.IsTrue(new Type[] { typeof(IA), typeof(IB) }.ContentEquals(missing));
		}
	}
}