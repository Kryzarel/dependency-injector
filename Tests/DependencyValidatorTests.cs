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
		public void CircularDependencyTests()
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
			if (data.CircularDependencies != null && data.CircularDependencies.Count > 0)
			{
				Assert.IsTrue(data.CircularDependencies.TryGetValue(typeof(IA), out IReadOnlyList<Type>? path));
				Assert.IsTrue(new Type[] { typeof(ACircularDependsOnE), typeof(E), typeof(ACircularDependsOnE) }.ContentEquals(path));

				Assert.IsTrue(data.CircularDependencies.TryGetValue(typeof(IE), out path));
				Assert.IsTrue(new Type[] { typeof(E), typeof(ACircularDependsOnE), typeof(E) }.ContentEquals(path));
			}
		}

		[Test]
		// Given, When, Then
		public void CircularDependencyTests2()
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
			if (data.CircularDependencies != null && data.CircularDependencies.Count > 0)
			{
				Assert.IsTrue(data.CircularDependencies.TryGetValue(typeof(ICircular1DependsOn2), out IReadOnlyList<Type>? path));
				Assert.IsTrue(new Type[] { typeof(Circular1), typeof(Circular2), typeof(Circular1) }.ContentEquals(path));

				Assert.IsTrue(data.CircularDependencies.TryGetValue(typeof(ICircular2DependsOn3), out path));
				Assert.IsTrue(new Type[] { typeof(Circular2), typeof(Circular1), typeof(Circular2) }.ContentEquals(path));

				Assert.IsTrue(data.CircularDependencies.TryGetValue(typeof(ICircular3DependsOn1), out path));
				Assert.IsTrue(new Type[] { typeof(Circular3), typeof(Circular1), typeof(Circular2), typeof(Circular1) }.ContentEquals(path));
			}
		}

		[Test]
		// Given, When, Then
		public void MissingDependencyTests()
		{

		}
	}
}