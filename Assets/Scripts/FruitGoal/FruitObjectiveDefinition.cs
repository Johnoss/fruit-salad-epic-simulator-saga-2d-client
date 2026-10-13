using System;
using Tile;
using UnityEngine;

namespace FruitGoal
{
    /// <summary>
    /// One authored objective: which fruit a level asks for and how many of it.
    ///
    /// Serializable so the level set can hold a list of these once levels are wired up. Until then
    /// <see cref="PlaceholderLevelObjectives"/> supplies them from code.
    /// </summary>
    [Serializable]
    public class FruitObjectiveDefinition
    {
        public TileType Fruit;

        [Tooltip("How many of the fruit the level wants. Zero or less falls back to the default " +
                 "count on FruitGoalConfig, so a level can leave it alone and still be valid.")]
        public int RequiredCount;

        public FruitObjectiveDefinition()
        {
        }

        public FruitObjectiveDefinition(TileType fruit, int requiredCount)
        {
            Fruit = fruit;
            RequiredCount = requiredCount;
        }
    }
}
