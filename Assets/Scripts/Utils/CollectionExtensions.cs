using System.Collections.Generic;
using Random = System.Random;

namespace Utils
{
    public static class CollectionExtensions
    {
        private static readonly Random Rand = new();

        public static T RandomElement<T>(this T[] items)
        {
            return items[Rand.Next(0, items.Length)];
        }

        public static T RandomElement<T>(this List<T> items)
        {
            return items[Rand.Next(0, items.Count)];
        }
    }
}