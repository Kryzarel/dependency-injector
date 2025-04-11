namespace Kryz.DI.Tests
{
	public class InstanceCounter
	{
		public static int Count;
		public InstanceCounter() { Count++; }
		~InstanceCounter() { Count--; }
	}

	public class EmptyClass { }
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

	public interface ICircular1DependsOn2
	{
		[Inject]
		ICircular2DependsOn3 Circular { get; }
	}
	public interface ICircular2DependsOn3
	{
		[Inject]
		ICircular1DependsOn2 Circular { get; }
	}
	public interface ICircular3DependsOn1
	{
		[Inject]
		ICircular1DependsOn2 Circular { get; }
	}

	public class A : IA
	{
	}

	public class B : IB
	{
		public readonly IA A;

		public B(IA a)
		{
			A = a;
		}
	}

	public class C : IC
	{
		public readonly IA? A;
		public readonly IB? B;

		IA? IC.A => A;

		public C()
		{
		}

		[Inject]
		public C(IA a, IB b)
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
		public T1? One;
		public T2? Two;
		public T3? Three;

		[Inject]
		public void Inject123(T1 one, T2 two, T3 three)
		{
			One = one;
			Two = two;
			Three = three;
		}
	}

	public class ProtectedSubClassInject<T> : BaseClass<T>
	{
		public override T? Value { get; protected set; }
		public override T? Value2 { get; protected set; }
		public override T? Value3 { get; protected set; }

		protected override void Inject(T arg1)
		{
			Value = arg1;
		}
	}

	public abstract class BaseClass<T>
	{
		public abstract T? Value { get; protected set; }
		[Inject]
		public abstract T? Value2 { get; protected set; }
		[Inject]
		public abstract T? Value3 { get; protected set; }

		[Inject]
		protected abstract void Inject(T arg1);
	}

	public class Circular1 : ICircular1DependsOn2
	{
		ICircular2DependsOn3 ICircular1DependsOn2.Circular => Circular;

		public readonly ICircular2DependsOn3 Circular;

		public Circular1(ICircular2DependsOn3 circular)
		{
			Circular = circular;
		}
	}

	public class Circular2 : ICircular2DependsOn3
	{
		ICircular1DependsOn2 ICircular2DependsOn3.Circular => Circular;

		public readonly ICircular1DependsOn2 Circular;

		public Circular2(ICircular1DependsOn2 circular)
		{
			Circular = circular;
		}
	}

	public class Circular3 : ICircular3DependsOn1
	{
		ICircular1DependsOn2 ICircular3DependsOn1.Circular => Circular;

		public readonly ICircular1DependsOn2 Circular;

		public Circular3(ICircular1DependsOn2 circular)
		{
			Circular = circular;
		}
	}

	public class ACircularDependsOnE : IA
	{
		public readonly IE E;

		[Inject]
		public ACircularDependsOnE(IE e)
		{
			E = e;
		}
	}
}