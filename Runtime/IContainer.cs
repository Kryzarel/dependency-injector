using System;
using System.Collections.Generic;

namespace Kryz.DI
{
	public interface IContainer : IObjectResolver, ITypeResolver, IDisposable
	{
		IInjector Injector { get; }
		IContainer? Parent { get; }
		IReadOnlyList<IContainer> ChildScopes { get; }

		IContainer CreateScope();
		IContainer CreateScope(Action<Builder> build);
	}
}