namespace Kryz.DI.Tests
{
	// TODO: Delete this after fixing Unity DI package
	public static class ContainerTestHelper
	{
		public static IContainer GetContainerWithRegistrations(Lifetime lifetime)
		{
			Builder builder = new();
			ContainerTests.Register(builder, ContainerTests.SafeTypes, lifetime);
			return builder.Build();
		}
	}
}