using System;
using System.Collections.Generic;

namespace Kryz.DI
{
	public class Container : ITypeResolver
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
		private readonly ReflectionInjector reflectionInjector;

		public Container()
		{
			Root = this;
			Parent = null;
			Children = children;
			reflectionInjector = new ReflectionInjector();
		}

		private Container(Container parent, ReflectionInjector injector)
		{
			Root = parent.Root;
			Parent = parent;
			Children = children;
			reflectionInjector = injector;
		}

		public Container AddChild()
		{
			Container child = new(this, reflectionInjector);
			children.Add(child);
			return child;
		}

		public bool RemoveChild(Container child)
		{
			return children.Remove(child);
		}

		public object Get(Type type)
		{
			if (TryGet(type, out object? obj))
			{
				return obj!;
			}
			throw new InjectionException($"Failed to get registration for type {type.FullName}");
		}

		public bool TryGet(Type type, out object? obj)
		{
			return TryGet(this, type, out obj);
		}

		public T Get<T>()
		{
			return (T)Get(typeof(T))!;
		}

		public bool TryGet<T>(out T? obj)
		{
			if (TryGet(typeof(T), out object? o))
			{
				obj = (T)o!; // We should assume this is NOT null here. If it is, something went horribly wrong.
				return true;
			}
			obj = default;
			return false;
		}

		public void Inject<T>(T obj) where T : notnull
		{
			reflectionInjector.Inject(typeof(T), obj, this);
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
			registrations[typeof(TBase)] = new Registration(typeof(TDerived));
			return this;
		}

		public Container AddSingleton<TBase, TDerived>(TDerived obj) where TDerived : notnull, TBase
		{
			return Root.AddScoped<TBase, TDerived>(obj);
		}

		public Container AddScoped<TBase, TDerived>(TDerived obj) where TDerived : notnull, TBase
		{
			objects[typeof(TDerived)] = obj;
			registrations[typeof(TBase)] = new Registration(typeof(TDerived));
			return this;
		}

		public Container AddTransient<TBase, TDerived>() where TDerived : TBase
		{
			registrations[typeof(TBase)] = new Registration(typeof(TDerived), true);
			return this;
		}

		public void Instantiate()
		{
			foreach (Type item in registrations.Keys)
			{
				TryGet(item, out _);
			}
		}

		private static bool TryGet(Container? container, Type type, out object? obj)
		{
			while (container != null)
			{
				if (container.TryGet_Internal(type, out obj))
				{
					return true;
				}
				container = container.Parent;
			}
			obj = null;
			return false;
		}

		private bool TryGet_Internal(Type type, out object? obj)
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

		private object CreateAndInject(Type type)
		{
			object obj = reflectionInjector.CreateObject(type, this);
			reflectionInjector.Inject(type, obj, this);
			return obj;
		}
	}
}