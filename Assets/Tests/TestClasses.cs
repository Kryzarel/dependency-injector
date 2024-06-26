namespace Kryz.DI.Tests
{
	public class Empty { }
	public struct EmptyStruct { }

	public interface IA { }
	public interface IB { }
	public interface IC
	{
		[Inject]
		IA A { get; }
	}
	public interface ID
	{
		[Inject]
		IB B { get; }
		[Inject]
		void InjectC(IC c);
	}

	public class A : IA
	{
	}

	public class B : IB
	{
		public readonly A A;

		public B(A a)
		{
			A = a;
		}
	}

	public class C : IC
	{
		public readonly IA A;
		public readonly B B;

		IA IC.A => A;

		public C()
		{
		}

		[Inject]
		public C(IA a, B b)
		{
			A = a;
			B = b;
		}
	}

	public class D : ID
	{
		[Inject]
		public IA A;
		[Inject]
		public IB B { get; set; }
		public IC C;

		[Inject]
		public void InjectC(IC c)
		{
			C = c;
		}
	}
}