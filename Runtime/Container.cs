using System;
using System.Collections.Generic;
using Kryz.DI.Reflection;

namespace Kryz.DI
{
	public class Container : IResolver
	{
		private readonly struct Registration
		{
			public readonly Type Type;
			public readonly bool Transient;

			public Registration(Type type, bool transient = false)
			{
				Type = type;
				Transient = transient;
			}
		}

		public readonly Container Root;
		public readonly Container? Parent;
		public readonly IReadOnlyList<Container> Children;

		private readonly List<Container> children = new();
		private readonly Dictionary<Type, object> objects = new();
		private readonly Dictionary<Type, Registration> registrations = new();
		private readonly IInjector injector;

		public Container()
		{
			Root = this;
			Parent = null;
			Children = children;
			injector = new ReflectionInjector();
		}

		private Container(Container parent)
		{
			Root = parent.Root;
			Parent = parent;
			Children = children;
			injector = parent.injector;
			parent.children.Add(this);
		}

		public void Clear()
		{
			children.Clear();
			objects.Clear();
			registrations.Clear();
		}

		public Container CreateChild()
		{
			return new Container(this);
		}

		public bool RemoveChild(Container child)
		{
			return children.Remove(child);
		}

		public T GetObject<T>()
		{
			return (T)GetObject(typeof(T))!;
		}

		public object GetObject(Type type)
		{
			if (TryGetObject(type, out object? obj))
			{
				return obj!;
			}
			throw new InjectionException($"No object for type {type.FullName} has been registered.");
		}

		public bool TryGetObject<T>(out T? obj)
		{
			if (TryGetObject(typeof(T), out object? o))
			{
				obj = (T)o!; // We should assume this is NOT null here. If it is, something went horribly wrong.
				return true;
			}
			obj = default;
			return false;
		}

		public bool TryGetObject(Type type, out object? obj)
		{
			return TryGetObject_Internal(this, type, out obj);
		}

		public Type GetType<T>()
		{
			return GetType(typeof(T));
		}

		public bool TryGetType<T>(out Type? type)
		{
			return TryGetType(typeof(T), out type);
		}

		public Type GetType(Type type)
		{
			if (TryGetType(type, out Type? t))
			{
				return t!;
			}
			throw new InjectionException($"Type {type.FullName} has not been registered.");
		}

		public bool TryGetType(Type type, out Type? resolvedType)
		{
			return TryGetType_Internal(this, type, out resolvedType);
		}

		public Container AddSingleton<T>(T obj) where T : notnull => AddSingleton<T, T>(obj);
		public Container AddScoped<T>(T obj) where T : notnull => AddScoped<T, T>(obj);
		public Container AddTransient<T>() => AddTransient<T, T>();

		public Container AddSingleton<TBase, TDerived>() where TDerived : TBase
		{
			return Root.AddScoped<TBase, TDerived>();
		}

		public Container AddScoped<TBase, TDerived>() where TDerived : TBase
		{
			Type type = typeof(TDerived);
			registrations[typeof(TBase)] = new Registration(type);
			if (injector.HasCircularDependency(type, this, out Type? circType))
			{
				throw new CircularDependencyException($"Can't register type {type.Name} because {circType?.Name} has a circular dependency on it.");
			}
			return this;
		}

		public Container AddSingleton<TBase, TDerived>(TDerived obj) where TDerived : notnull, TBase
		{
			return Root.AddScoped<TBase, TDerived>(obj);
		}

		public Container AddScoped<TBase, TDerived>(TDerived obj) where TDerived : notnull, TBase
		{
			// No need to check circular dependencies here because we already have the instantiated object
			objects[typeof(TDerived)] = obj;
			registrations[typeof(TBase)] = new Registration(typeof(TDerived));
			return this;
		}

		public Container AddTransient<TBase, TDerived>() where TDerived : TBase
		{
			Type type = typeof(TDerived);
			registrations[typeof(TBase)] = new Registration(type, true);
			if (injector.HasCircularDependency(type, this, out Type? circType))
			{
				throw new CircularDependencyException($"Can't register type {type.Name} because {circType?.Name} has a circular dependency on it.");
			}
			return this;
		}

		public void Instantiate()
		{
			foreach (KeyValuePair<Type, Registration> item in registrations)
			{
				if (!item.Value.Transient)
				{
					TryGetObject(item.Key, out _);
				}
			}
		}

		public void Inject<T>(T obj) where T : notnull
		{
			injector.Inject(obj, this);
		}

		private object CreateAndInject(Type type)
		{
			object obj = injector.CreateObject(type, this);
			injector.Inject(obj, this);
			return obj;
		}

		private static bool TryGetType_Internal(Container? container, Type type, out Type? resolvedType)
		{
			while (container != null)
			{
				if (container.registrations.TryGetValue(type, out Registration registration))
				{
					resolvedType = registration.Type;
					return true;
				}
				container = container.Parent;
			}
			resolvedType = null;
			return false;
		}

		private static bool TryGetObject_Internal(Container? container, Type type, out object? obj)
		{
			while (container != null)
			{
				if (container.TryGetObject_Internal(type, out obj))
				{
					return true;
				}
				container = container.Parent;
			}
			obj = null;
			return false;
		}

		private bool TryGetObject_Internal(Type type, out object? obj)
		{
			if (registrations.TryGetValue(type, out Registration registration))
			{
				if (registration.Transient)
				{
					obj = CreateAndInject(registration.Type);
				}
				else if (!objects.TryGetValue(registration.Type, out obj))
				{
					obj = objects[registration.Type] = CreateAndInject(registration.Type);
				}
				return true;
			}
			obj = null;
			return false;
		}
	}
}