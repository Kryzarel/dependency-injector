using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kryz.DI
{
	public static class TypeExtensions
	{
		/// <summary>
		/// C# doesn't give you base class private members even if you use BindingFlags.NonPublic.
		/// We have to manually search the base class(es) to find them.
		/// </summary>
		public static void GetAllFields(this Type type, BindingFlags bindingFlags, List<FieldInfo> fieldInfos)
		{
			do
			{
				// Use BindingFlags.DeclaredOnly to prevent including duplicate fields from base/derived classes
				fieldInfos.AddRangeNonAlloc(type.GetFields(bindingFlags | BindingFlags.DeclaredOnly));
				type = type.BaseType;
			}
			while (type != null);
		}

		/// <summary>
		/// C# doesn't give you base class private members even if you use BindingFlags.NonPublic.
		/// We have to manually search the base class(es) to find them.
		/// </summary>
		public static void GetAllProperties(this Type type, BindingFlags bindingFlags, List<PropertyInfo> propertyInfos)
		{
			do
			{
				// Use BindingFlags.DeclaredOnly to prevent including duplicate fields from base/derived classes
				propertyInfos.AddRangeNonAlloc(type.GetProperties(bindingFlags | BindingFlags.DeclaredOnly));
				type = type.BaseType;
			}
			while (type != null);
		}

		/// <summary>
		/// C# doesn't give you base class private members even if you use BindingFlags.NonPublic.
		/// We have to manually search the base class(es) to find them.
		/// </summary>
		public static void GetAllMethods(this Type type, BindingFlags bindingFlags, List<MethodInfo> methodInfos)
		{
			do
			{
				// Use BindingFlags.DeclaredOnly to prevent including duplicate fields from base/derived classes
				methodInfos.AddRangeNonAlloc(type.GetMethods(bindingFlags | BindingFlags.DeclaredOnly));
				type = type.BaseType;
			}
			while (type != null);
		}

		/// <summary>
		/// C# doesn't give you base class private members even if you use BindingFlags.NonPublic.
		/// We have to manually search the base class(es) to find them.
		/// </summary>
		public static void GetAllFieldsWithAttribute(this Type type, BindingFlags bindingFlags, Type attributeType, List<FieldInfo> fieldInfos)
		{
			do
			{
				// Use BindingFlags.DeclaredOnly to prevent including duplicate fields from base/derived classes
				fieldInfos.AddRangeWhere(type.GetFields(bindingFlags | BindingFlags.DeclaredOnly), item => item.IsDefined(attributeType));
				type = type.BaseType;
			}
			while (type != null);
		}

		/// <summary>
		/// C# doesn't give you base class private members even if you use BindingFlags.NonPublic.
		/// We have to manually search the base class(es) to find them.
		/// </summary>
		public static void GetAllPropertiesWithAttribute(this Type type, BindingFlags bindingFlags, Type attributeType, List<PropertyInfo> propertyInfos)
		{
			do
			{
				// Use BindingFlags.DeclaredOnly to prevent including duplicate fields from base/derived classes
				propertyInfos.AddRangeWhere(type.GetProperties(bindingFlags | BindingFlags.DeclaredOnly), item => item.IsDefined(attributeType));
				type = type.BaseType;
			}
			while (type != null);
		}

		/// <summary>
		/// C# doesn't give you base class private members even if you use BindingFlags.NonPublic.
		/// We have to manually search the base class(es) to find them.
		/// </summary>
		public static void GetAllMethodsWithAttribute(this Type type, BindingFlags bindingFlags, Type attributeType, List<MethodInfo> methodInfos)
		{
			do
			{
				// Use BindingFlags.DeclaredOnly to prevent including duplicate fields from base/derived classes
				methodInfos.AddRangeWhere(type.GetMethods(bindingFlags | BindingFlags.DeclaredOnly), item => item.IsDefined(attributeType));
				type = type.BaseType;
			}
			while (type != null);
		}
	}
}