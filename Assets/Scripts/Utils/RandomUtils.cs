using System;

namespace Utils
{
    public static class RandomUtils
    {
        public static readonly Random Rand = new();

        /// <summary>
        /// Box-Muller transform. System.Random only produces a uniform distribution, so a normal
        /// one has to be derived from two uniform samples.
        /// </summary>
        public static float NextGaussian(float mean, float standardDeviation)
        {
            // 1.0 - NextDouble() shifts the range to (0, 1]; Log(0) is undefined.
            var uniform1 = 1.0 - Rand.NextDouble();
            var uniform2 = 1.0 - Rand.NextDouble();

            var standardNormal = Math.Sqrt(-2.0 * Math.Log(uniform1)) * Math.Sin(2.0 * Math.PI * uniform2);

            return (float)(mean + standardDeviation * standardNormal);
        }
    }
}
