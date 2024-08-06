using System;
using System.Collections.Generic;

namespace Kryz.DI
{
	public class Container : ITypeResolver
	{
		private readonly struct Registration
		{
			public readonly Type Type;
			public readonly object? Object;
			public readonly bool Transient;

			public Registration(Type type, object? obj, bool transient = false)
			{
				Type = type;
				Object = obj;
				Transient = transient;
			}
		}

		public readonly Container Root;
		public readonly Container? Parent;
		public readonly IReadOnlyList<Container> Children;

		private readonly List<Container> children = new();
		private readonly Dictionary<Type, Registration> objects = new();
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
			Container? container = this;
			while (container != null)
			{
				if (container.objects.TryGetValue(type, out Registration registration))
				{
					obj = registration.Object ?? CreateAndInject(registration.Type);
					if (registration.Object == null && !registration.Transient)
					{
						container.objects[type] = new Registration(registration.Type, obj);
					}
					return true;
				}
				container = Parent;
			}
			obj = null;
			return false;
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

		public Container AddSingleton<T>(T obj) => AddSingleton<T, T>(obj);
		public Container AddScoped<T>(T obj) => AddScoped<T, T>(obj);
		public Container AddTransient<T>() => AddTransient<T, T>();

		public Container AddSingleton<TBase, TDerived>(bool lazy = false) where TDerived : TBase
		{
			return AddSingleton<TBase, TDerived>(lazy ? default : CreateAndInject<TDerived>());
		}

		public Container AddScoped<TBase, TDerived>(bool lazy = false) where TDerived : TBase
		{
			return AddScoped<TBase, TDerived>(lazy ? default : CreateAndInject<TDerived>());
		}

		public Container AddSingleton<TBase, TDerived>(TDerived? obj) where TDerived : TBase
		{
			Root.objects[typeof(TBase)] = new Registration(typeof(TDerived), obj);
			return this;
		}

		public Container AddScoped<TBase, TDerived>(TDerived? obj) where TDerived : TBase
		{
			objects[typeof(TBase)] = new Registration(typeof(TDerived), obj);
			return this;
		}

		public Container AddTransient<TBase, TDerived>() where TDerived : TBase
		{
			objects[typeof(TBase)] = new Registration(typeof(TDerived), null, true);
			return this;
		}

		private object CreateAndInject(Type type)
		{
			object obj = reflectionInjector.CreateObject(type, this);
			reflectionInjector.Inject(type, obj, this);
			return obj;
		}

		private T CreateAndInject<T>() => (T)CreateAndInject(typeof(T));
	}
}