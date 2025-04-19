namespace Kryz.DI
{
	public interface IBuilder : IScopeBuilder
	{
		IContainer Build();
	}
}