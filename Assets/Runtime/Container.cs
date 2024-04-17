using System;
using System.Collections.Generic;

namespace Kryz.DI
{
	public class Container : ITypeResolver
	{
		public readonly Container Parent;
		public readonly IList<Container> Children;

		private readonly List<Container> children = new();
		private readonly Dictionary<Type, object> objects = new();

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

		public T Get<T>()
		{
			return (T)objects[typeof(T)];
		}

		public bool TryGet<T>(out T obj)
		{
			if (objects[typeof(T)] is T o)
			{
				obj = o;
				return true;
			}
			obj = default;
			return false;
		}

		public object Get(Type type) => objects[type];

		public bool TryGet(Type type, out object obj) => objects.TryGetValue(type, out obj);

		public Container AddSingleton<T>(T obj) => AddSingleton<T, T>(obj);
		public Container AddScoped<T>(T obj) => AddScoped<T, T>(obj);
		public Container AddTransient<T>() => AddTransient<T, T>();

		public Container AddSingleton<TBase, TDerived>(TDerived obj) where TDerived : TBase
		{
			GetRootContainer().objects[typeof(TBase)] = obj;
			return this;
		}

		public Container AddScoped<TBase, TDerived>(TDerived obj) where TDerived : TBase
		{
			objects[typeof(TBase)] = obj;
			return this;
		}

		public Container AddTransient<TBase, TDerived>() where TDerived : TBase
		{
			objects[typeof(TBase)] = null;
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
	}
}