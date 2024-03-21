using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public static class Vector2IntExtensions
    {
        public static bool IsAdjacentTo(this Vector2Int vector1, Vector2Int vector2)
        {
            var vectorDifference = vector1 - vector2;
            return Math.Abs(vectorDifference.x) <= 1 && Math.Abs(vectorDifference.y) <= 1;
        }

        public static IEnumerable<Vector2Int> GetAdjacentCoordinates(this Vector2Int coordinates, Vector2Int bounds,
            HashSet<Vector2Int> refuseCoordinates = null)
        {
            var adjacentCoordinates = new List<Vector2Int>();
            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    var adjacentCoordinate = coordinates + new Vector2Int(x, y);
                    
                    if (x == 0 && y == 0
                        || !adjacentCoordinate.IsWithinBounds(bounds)
                        || refuseCoordinates != null && refuseCoordinates.Contains(adjacentCoordinate))
                    {
                        continue;
                    }

                    adjacentCoordinates.Add(adjacentCoordinate);
                }
            }

            return adjacentCoordinates;
        }

        private static bool IsWithinBounds(this Vector2Int coordinate, Vector2Int gridResolution)
        {
            var xWithingBounds = coordinate.x < gridResolution.x && coordinate.x >= 0; 
            var yWithingBounds = coordinate.y < gridResolution.y && coordinate.y >= 0;
            return xWithingBounds && yWithingBounds;
        }
    }
}