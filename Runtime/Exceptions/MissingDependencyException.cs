using System;

namespace Kryz.DI
{
	public class MissingDependencyException : Exception
	{
		public MissingDependencyException() : base() { }
		public MissingDependencyException(string message) : base(message) { }
		public MissingDependencyException(string message, Exception innerException) : base(message, innerException) { }
	}
}