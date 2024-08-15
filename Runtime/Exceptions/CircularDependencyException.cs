using System;

namespace Kryz.DI
{
	public class CircularDependencyException : Exception
	{
		public CircularDependencyException() : base() { }
		public CircularDependencyException(string message) : base(message) { }
		public CircularDependencyException(string message, Exception innerException) : base(message, innerException) { }
	}
}