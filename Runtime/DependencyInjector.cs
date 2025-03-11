namespace Kryz.DI
{
	public static class DependencyInjector
	{
		public static readonly IContainer RootContainer = new Builder().Build_Internal();
	}
}