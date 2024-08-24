using System;

namespace Kryz.DI.Tests
{
	public static class ContainerTestHelper
	{
		public enum RegisterType { Singleton, Scoped, Transient }

		public static Container Add<TBase, TDerived>(Container container, RegisterType registerType) where TDerived : TBase
		{
			return registerType switch
			{
				RegisterType.Singleton => container.AddSingleton<TBase, TDerived>(),
				RegisterType.Scoped => container.AddScoped<TBase, TDerived>(),
				RegisterType.Transient => container.AddTransient<TBase, TDerived>(),
				_ => throw new NotImplementedException(),
			};
		}

		public static Container SetupContainer(Container container, RegisterType registerType)
		{
			Add<Empty, Empty>(container, registerType);
			Add<IA, A>(container, registerType);
			Add<IB, B>(container, registerType);
			Add<IC, C>(container, registerType);
			Add<ID, D>(container, registerType);
			Add<IE, E>(container, registerType);
			Add<IGeneric<IA, IB, IC>, Generic<IA, IB, IC>>(container, registerType);
			Add<IGeneric<ID, IE, Empty>, Generic<ID, IE, Empty>>(container, registerType);
			Add<ICircular1, Circular1>(container, registerType);
			Add<ICircular2, Circular2>(container, registerType);
			Add<ICircular1NoInject, Circular1NoInject>(container, registerType);
			Add<ICircular2NoInject, Circular2NoInject>(container, registerType);
			return container;
		}
	}
}