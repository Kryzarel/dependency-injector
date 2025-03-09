using System;
using System.Collections.Generic;

namespace Kryz.DI
{
	public interface IInjector
	{
		object CreateObject(Type type, IResolver typeResolver);
		void Inject(object obj, IResolver typeResolver);
		IReadOnlyList<Type> GetDependencies(Type type);
	}
}