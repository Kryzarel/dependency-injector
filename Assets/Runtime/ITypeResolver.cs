using System;

namespace Kryz.DI
{
	public interface ITypeResolver
	{
		T Get<T>();
		object Get(Type type);

		bool TryGet<T>(out T? obj);
		bool TryGet(Type type, out object? obj);
	}
}