namespace Kryz.DI.Tests
{
	public class Empty { }
	public struct EmptyStruct { }

	public class A
	{
		public A()
		{
		}
	}

	public class B
	{
		public readonly A A;

		public B(A a)
		{
			A = a;
		}
	}

	public class C
	{
		public readonly A A;
		public readonly B B;

		public C()
		{
		}

		[Inject]
		public C(A a, B b)
		{
			A = a;
			B = b;
		}
	}

	public class D
	{
		[Inject]
		public A A;
		[Inject]
		public B B { get; set; }
		public C C;

		[Inject]
		public void InjectC(C c)
		{
			C = c;
		}
	}
}