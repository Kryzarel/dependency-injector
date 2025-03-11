using System;
using System.Collections.Generic;

namespace Kryz.DI
{
	public interface IInjector
	{
		object CreateObject(Type type, IObjectResolver resolver);
		void Inject(object obj, IObjectResolver resolver);
		IReadOnlyList<Type> GetDependencies(Type type);
	}
}