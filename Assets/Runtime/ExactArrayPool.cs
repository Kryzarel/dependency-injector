using System;
using System.Buffers;
using System.Collections.Generic;

namespace Kryz.DI
{
	public class ExactArrayPool<T> : ArrayPool<T>
	{
		public new static readonly ExactArrayPool<T> Shared = new();

		private readonly Dictionary<int, List<T[]>> pools = new();

		public override T[] Rent(int minimumLength)
		{
			if (!pools.TryGetValue(minimumLength, out List<T[]> pool))
			{
				pools[minimumLength] = pool = new List<T[]>();
			}

			if (pool.Count > 0)
			{
				T[] array = pool[pool.Count - 1];
				pool.RemoveAt(pool.Count - 1);
				return array;
			}
			return new T[minimumLength];
		}

		public override void Return(T[] array, bool clearArray = false)
		{
			if (!pools.TryGetValue(array.Length, out List<T[]> pool))
			{
				pools[array.Length] = pool = new List<T[]>();
			}

			if (clearArray)
			{
				Array.Clear(array, 0, array.Length);
			}
			pool.Add(array);
		}
	}
}