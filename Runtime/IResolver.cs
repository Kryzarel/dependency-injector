using System;

namespace Kryz.DI
{
	public interface IResolver
	{
		/// <summary>
		///	Get or create an object of the requested type. An exception is thrown if the type has not been registered.
		/// </summary>
		T GetObject<T>();
		/// <summary>
		///	Get or create an object of the requested type. An exception is thrown if the type has not been registered.
		/// </summary>
		object GetObject(Type type);

		/// <summary>
		///	Get or create an object of the requested type. Returns false if the type has not been registered.
		/// </summary>
		bool TryGetObject<T>(out T? obj);
		/// <summary>
		///	Get or create an object of the requested type. Returns false if the type has not been registered.
		/// </summary>
		bool TryGetObject(Type type, out object? obj);

		/// <summary>
		/// Check if an object exists without instanting one.
		/// </summary>
		bool ContainsObject<T>();
		/// <summary>
		/// Check if an object exists without instanting one.
		/// </summary>
		bool ContainsObject(Type type);

		/// <summary>
		/// Get the type registered to the reguested type. An exception is thrown if the type has not been registered.
		/// </summary>
		Type GetType<T>();
		/// <summary>
		/// Get the type registered to the reguested type. An exception is thrown if the type has not been registered.
		/// </summary>
		Type GetType(Type type);

		/// <summary>
		/// Get the type registered to the reguested type. Returns false if the type has not been registered.
		/// </summary>
		bool TryGetType<T>(out Type? type);
		/// <summary>
		/// Get the type registered to the reguested type. Returns false if the type has not been registered.
		/// </summary>
		bool TryGetType(Type type, out Type? resolvedType);
	}
}