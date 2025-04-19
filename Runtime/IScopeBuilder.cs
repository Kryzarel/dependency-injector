namespace Kryz.DI
{
	public interface IScopeBuilder
	{
		IBuilder Register<T>(Lifetime lifetime);
		IBuilder Register<TBase, TDerived>(Lifetime lifetime) where TDerived : TBase;
		IBuilder Register<T>(T obj) where T : notnull;
	}
}