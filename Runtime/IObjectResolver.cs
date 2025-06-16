using System;
using System.Diagnostics.CodeAnalysis;

namespace Kryz.DI
{
	public interface IObjectResolver
	{
		/// <summary>
		///	Get or create an object registered as the requested type. An exception is thrown if the type has not been registered.
		/// </summary>
		T ResolveObject<T>();
		/// <summary>
		///	Get or create an object registered as the requested type. An exception is thrown if the type has not been registered.
		/// </summary>
		object ResolveObject(Type type);

		/// <summary>
		///	Get or create an object registered as the requested type. Returns false if the type has not been registered.
		/// </summary>
		bool TryResolveObject<T>([MaybeNullWhen(returnValue: false)] out T obj);
		/// <summary>
		///	Get or create an object registered as the requested type. Returns false if the type has not been registered.
		/// </summary>
		bool TryResolveObject(Type type, [MaybeNullWhen(returnValue: false)] out object obj);
	}
}