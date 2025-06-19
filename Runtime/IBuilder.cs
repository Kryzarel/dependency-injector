namespace Kryz.DI
{
	public interface IBuilder : IRegister
	{
		IContainer Build();
	}
}