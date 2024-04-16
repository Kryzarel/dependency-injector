using System;
using System.Collections.Generic;

namespace Kryz.DI
{
	public class Container
	{
		private readonly struct Registration
		{
			public readonly object Object;
			public readonly Type Type;

			public Registration(object obj, Type type)
			{
				Object = obj;
				Type = type;
			}
		}

		public readonly Container Parent;
		public readonly IList<Container> Children;

		private readonly List<Container> children = new();
		private readonly Dictionary<Type, Registration> objects = new();

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

		public Container AddSingleton<TBase, TDerived>(TDerived obj) where TDerived : TBase
		{
			GetRootContainer().objects[typeof(TBase)] = new Registration(obj, typeof(TDerived));
			return this;
		}

		public Container AddScoped<TBase, TDerived>(TDerived obj) where TDerived : TBase
		{
			objects[typeof(TBase)] = new Registration(obj, typeof(TDerived));
			return this;
		}

		public Container AddTransient<TBase, TDerived>() where TDerived : TBase
		{
			objects[typeof(TBase)] = new Registration(null, typeof(TDerived));
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