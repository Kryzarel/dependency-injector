using System;
using System.Collections.Generic;

namespace Kryz.DI
{
	public class Container : ITypeResolver
	{
		private readonly struct Registration
		{
			public readonly Type Type;
			public readonly object Value;

			public Registration(Type type, object value)
			{
				Type = type;
				Value = value;
			}
		}

		public readonly Container Parent;
		public readonly IList<Container> Children;

		private readonly List<Container> children = new();
		private readonly Dictionary<Type, Registration> objects = new();
		private readonly ReflectionInjector reflectionInjector = new();

		public Container(Container parent = null)
		{
			Parent = parent;
			Children = children;
		}

		public Container AddChild()
		{
			Container child = new(this);
			children.Add(child);
			return child;
		}

		public object Get(Type type)
		{
			Container container = this;
			while (container != null)
			{
				if (container.objects.TryGetValue(type, out Registration registration))
				{
					return registration.Value ?? CreateAndInject(registration.Type);
				}
				container = Parent;
			}
			throw new InjectionException($"Failed to get registration for type {type.FullName}");
		}

		public bool TryGet(Type type, out object obj)
		{
			Container container = this;
			while (container != null)
			{
				if (container.objects.TryGetValue(type, out Registration registration))
				{
					obj = registration.Value ?? CreateAndInject(registration.Type);
					return true;
				}
				container = Parent;
			}
			obj = null;
			return false;
		}

		public T Get<T>()
		{
			return (T)Get(typeof(T));
		}

		public bool TryGet<T>(out T obj)
		{
			if (TryGet(typeof(T), out object o))
			{
				obj = (T)o;
				return true;
			}
			obj = default;
			return false;
		}

		public Container AddSingleton<T>(T obj) => AddSingleton<T, T>(obj);
		public Container AddScoped<T>(T obj) => AddScoped<T, T>(obj);
		public Container AddTransient<T>() => AddTransient<T, T>();

		public Container AddSingleton<TBase, TDerived>() where TDerived : TBase
		{
			return AddSingleton<TBase, TDerived>((TDerived)CreateAndInject(typeof(TDerived)));
		}

		public Container AddScoped<TBase, TDerived>() where TDerived : TBase
		{
			return AddScoped<TBase, TDerived>((TDerived)CreateAndInject(typeof(TDerived)));
		}

		public Container AddSingleton<TBase, TDerived>(TDerived obj) where TDerived : TBase
		{
			GetRootContainer().objects[typeof(TBase)] = new Registration(typeof(TDerived), obj);
			return this;
		}

		public Container AddScoped<TBase, TDerived>(TDerived obj) where TDerived : TBase
		{
			objects[typeof(TBase)] = new Registration(typeof(TDerived), obj);
			return this;
		}

		public Container AddTransient<TBase, TDerived>() where TDerived : TBase
		{
			objects[typeof(TBase)] = new Registration(typeof(TDerived), null);
			return this;
		}

		private Container GetRootContainer()
		{
			Container container = this;
			while (container.Parent != null)
			{
				container = container.Parent;
			}
			return container;
		}

		private object CreateAndInject(Type type)
		{
			object obj = reflectionInjector.CreateObject(type, this);
			reflectionInjector.Inject(type, obj, this);
			return obj;
		}
	}
}