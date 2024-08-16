using System;
using System.Collections.Generic;

namespace Kryz.DI
{
	public static class CollectionExtensions
	{
		public static void EnsureCapacity<T>(this List<T> list, int capacity)
		{
			int currentCapacity = list.Capacity;
			if (capacity > currentCapacity)
			{
				list.Capacity = Math.Max(capacity, currentCapacity * 2);
			}
		}

		public static void AddRangeNonAlloc<T>(this List<T> list, IReadOnlyList<T> toAdd)
		{
			int count = toAdd.Count;
			list.EnsureCapacity(list.Count + count);

			for (int i = 0; i < count; i++)
			{
				list.Add(toAdd[i]);
			}
		}

		public static void AddRangeWhere<T>(this List<T> list, IReadOnlyList<T> toAdd, Func<T, bool> predicate)
		{
			int count = toAdd.Count;
			list.EnsureCapacity(list.Count + count);

			for (int i = 0; i < count; i++)
			{
				T item = toAdd[i];
				if (predicate(item))
				{
					list.Add(item);
				}
			}
		}
	}
}