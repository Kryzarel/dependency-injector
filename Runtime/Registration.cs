using System;

namespace Kryz.DI
{
	internal readonly struct Registration
	{
		public readonly Type Type;
		public readonly object? Object;
		public readonly Lifetime Lifetime;

		public Registration(Type type, object? obj, Lifetime lifetime)
		{
			Type = type;
			Object = obj;
			Lifetime = lifetime;
		}
	}
}