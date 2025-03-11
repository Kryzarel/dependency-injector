namespace Kryz.DI.Tests
{
	public static class ContainerTestHelper
	{
		public static IContainer SetupContainer(Lifetime lifetime)
		{
			Builder builder = new();
			Register(builder, lifetime);
			return builder.Build();
		}

		public static void Register(Builder builder, Lifetime lifetime)
		{
			builder.Register<Empty, Empty>(lifetime);
			builder.Register<IA, A>(lifetime);
			builder.Register<IB, B>(lifetime);
			builder.Register<IC, C>(lifetime);
			builder.Register<ID, D>(lifetime);
			builder.Register<IE, E>(lifetime);
			builder.Register<IGeneric<IA, IB, IC>, Generic<IA, IB, IC>>(lifetime);
			builder.Register<IGeneric<ID, IE, Empty>, Generic<ID, IE, Empty>>(lifetime);
		}
	}
}