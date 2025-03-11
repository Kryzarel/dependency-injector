namespace Kryz.DI
{
	public interface IContainer : IResolver
	{
		IContainer? Parent { get; }
		IInjector Injector { get; }
	}
}