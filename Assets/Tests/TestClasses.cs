namespace Kryz.DI.Tests
{
	public class Empty { }
	public struct EmptyStruct { }

	public interface IA { }
	public interface IB { }
	public interface IC
	{
		[Inject]
		IA? A { get; }
	}
	public interface ID
	{
		[Inject]
		IB? B { get; }
		[Inject]
		void InjectC(IC c);
	}
	public interface IE
	{
		[Inject]
		void InjectA(IA a);
		[Inject]
		void InjectBCD(IB b, IC c, ID d);
	}

	public interface IGeneric<T1, T2, T3>
	{
		[Inject]
		void Inject123(T1 a, T2 b, T3 c);
	}

	public interface ICircular1
	{
		[Inject]
		ICircular2 Circular { get; }
	}
	public interface ICircular2
	{
		[Inject]
		ICircular1 Circular { get; }
	}
	public interface ICircular1NoInject
	{
		ICircular2NoInject Circular { get; }
	}
	public interface ICircular2NoInject
	{
		ICircular1NoInject Circular { get; }
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
		public readonly IA? A;
		public readonly B? B;

		IA? IC.A => A;

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
		public IA? A;
		[Inject]
		public IB? B { get; set; }
		public IC? C;

		[Inject]
		public void InjectC(IC c)
		{
			C = c;
		}
	}

	public class E : IE
	{
		public IA? A;
		public IB? B;
		public IC? C;
		public ID? D;

		[Inject]
		public void InjectA(IA a)
		{
			A = a;
		}

		[Inject]
		public void InjectBCD(IB b, IC c, ID d)
		{
			B = b;
			C = c;
			D = d;
		}
	}

	public class Generic<T1, T2, T3> : IGeneric<T1, T2, T3>
	{
		public T1? A;
		public T2? B;
		public T3? C;

		[Inject]
		public void Inject123(T1 a, T2 b, T3 c)
		{
			A = a;
			B = b;
			C = c;
		}
	}

	public class Circular1 : ICircular1
	{
		ICircular2 ICircular1.Circular => Circular;

		public readonly ICircular2 Circular;

		public Circular1(ICircular2 circular)
		{
			Circular = circular;
		}
	}

	public class Circular2 : ICircular2
	{
		ICircular1 ICircular2.Circular => Circular;

		public readonly ICircular1 Circular;

		public Circular2(ICircular1 circular)
		{
			Circular = circular;
		}
	}

	public class Circular1NoInject : ICircular1NoInject
	{
		ICircular2NoInject ICircular1NoInject.Circular => Circular;

		public readonly ICircular2NoInject Circular;

		public Circular1NoInject(ICircular2NoInject circular)
		{
			Circular = circular;
		}
	}

	public class Circular2NoInject : ICircular2NoInject
	{
		ICircular1NoInject ICircular2NoInject.Circular => Circular;

		public readonly ICircular1NoInject Circular;

		public Circular2NoInject(ICircular1NoInject circular)
		{
			Circular = circular;
		}
	}

	public class Circular1Concrete : ICircular1
	{
		ICircular2 ICircular1.Circular => Circular;

		public readonly ICircular2 Circular;

		public Circular1Concrete(Circular2Concrete circular)
		{
			Circular = circular;
		}
	}

	public class Circular2Concrete : ICircular2
	{
		ICircular1 ICircular2.Circular => Circular;

		public readonly ICircular1 Circular;

		public Circular2Concrete(Circular1Concrete circular)
		{
			Circular = circular;
		}
	}
}