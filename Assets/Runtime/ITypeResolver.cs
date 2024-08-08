using System;

namespace Kryz.DI
{
	public interface ITypeResolver
	{
		T GetObject<T>();
		object GetObject(Type type);

		bool TryGetObject<T>(out T? obj);
		bool TryGetObject(Type type, out object? obj);

		Type GetType<T>();
		Type GetType(Type type);

		bool TryGetType<T>(out Type? type);
		bool TryGetType(Type type, out Type? resolvedType);
	}
}