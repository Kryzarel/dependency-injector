using UnityEngine;

namespace Kryz.DI
{
	public static class DependencyInjector
	{
		public static readonly Container RootContainer = new();

		static DependencyInjector()
		{
			Application.quitting += RootContainer.Clear;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			RootContainer.Clear();
		}
	}
}