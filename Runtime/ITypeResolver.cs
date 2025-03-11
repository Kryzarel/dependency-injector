using System;
using System.Diagnostics.CodeAnalysis;

namespace Kryz.DI
{
	public interface ITypeResolver
	{
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
		bool TryGetType<T>([MaybeNullWhen(returnValue: false)] out Type type);
		/// <summary>
		/// Get the type registered to the reguested type. Returns false if the type has not been registered.
		/// </summary>
		bool TryGetType(Type type, [MaybeNullWhen(returnValue: false)] out Type resolvedType);
	}
}