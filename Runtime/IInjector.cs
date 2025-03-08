using System;

namespace Kryz.DI
{
	public interface IInjector
	{
		void Inject(object obj, IResolver typeResolver);
		object CreateObject(Type type, IResolver typeResolver);
		bool HasCircularDependency(Type type, IResolver typeResolver, out Type? circType);
	}
}