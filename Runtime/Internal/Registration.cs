using System;

namespace Kryz.DI.Internal
{
	internal readonly struct Registration
	{
		public readonly Type Type;
		public readonly Lifetime Lifetime;

		public Registration(Type type, Lifetime lifetime)
		{
			Type = type;
			Lifetime = lifetime;
		}
	}
}