using System;

namespace Kryz.DI
{
	public class AbstractTypeException : Exception
	{
		public AbstractTypeException() : base() { }
		public AbstractTypeException(string message) : base(message) { }
		public AbstractTypeException(string message, Exception innerException) : base(message, innerException) { }
	}
}