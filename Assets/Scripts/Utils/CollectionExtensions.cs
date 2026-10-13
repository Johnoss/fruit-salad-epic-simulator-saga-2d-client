using System.Collections.Generic;

namespace Utils
{
    public static class CollectionExtensions
    {
        public static T RandomElement<T>(this T[] items)
        {
            return items[RandomUtils.Rand.Next(0, items.Length)];
        }

        public static T RandomElement<T>(this List<T> items)
        {
            return items[RandomUtils.Rand.Next(0, items.Count)];
        }

        /// <summary>
        /// For anything handed out as read-only, which is how the level exposes its fruit. The
        /// array and List overloads are more specific, so they still win where they apply.
        /// </summary>
        public static T RandomElement<T>(this IReadOnlyList<T> items)
        {
            return items[RandomUtils.Rand.Next(0, items.Count)];
        }

        /// Fisher-Yates, in place.
        public static void Shuffle<T>(this IList<T> items)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var swapIndex = RandomUtils.Rand.Next(0, i + 1);
                (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
            }
        }
    }
}