using System;
using System.Diagnostics.CodeAnalysis;

namespace Kryz.DI
{
	public interface ITypeResolver
	{
		/// <summary>
		/// Get the concrete type registered to the requested base type "T". An exception is thrown if the base type has not been registered.
		/// </summary>
		Type GetType<T>();
		/// <summary>
		/// Get the concrete type registered to the requested base type. An exception is thrown if the base type has not been registered.
		/// </summary>
		Type GetType(Type baseType);

		/// <summary>
		/// Get the concrete type registered to the requested base type "T". Returns false if the base type has not been registered.
		/// </summary>
		bool TryGetType<T>([MaybeNullWhen(returnValue: false)] out Type resolvedType);
		/// <summary>
		/// Get the concrete type registered to the requested base type. Returns false if the base type has not been registered.
		/// </summary>
		bool TryGetType(Type baseType, [MaybeNullWhen(returnValue: false)] out Type resolvedType);
	}
}