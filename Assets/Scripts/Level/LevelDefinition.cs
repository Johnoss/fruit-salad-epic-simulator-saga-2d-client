using System;
using System.Collections.Generic;
using FruitGoal;
using Tile;
using UnityEngine;

namespace Level
{
    /// <summary>
    /// One authored level: how big the board is, which fruit falls on it, and what the player has
    /// to collect.
    ///
    /// Serializable so this can move into a ScriptableObject level set the moment there are more
    /// levels than fit comfortably in <see cref="LevelSet"/>. Everything downstream already takes
    /// an <see cref="IReadOnlyList{T}"/>, so that move touches nothing but the source.
    /// </summary>
    [Serializable]
    public class LevelDefinition
    {
        [Tooltip("Rows in x, columns in y - the grid layout constrains on columns.")]
        public Vector2Int BoardResolution = new(7, 7);

        [Tooltip("Which fruit the board spawns. Fewer fruit is an easier level, because matches are " +
                 "easier to find. Empty falls back to every spawnable fruit in TileConfig.\n\n" +
                 "Listing a fruit more than once makes it that much more common - the pool is drawn " +
                 "from uniformly, so repeats are the weighting. Starfruit is allowed here and spawns " +
                 "as a normal wildcard when it is.")]
        public List<TileType> Fruits = new();

        [Tooltip("Wildcards dropped onto the board before the player touches it, placed by the same " +
                 "centre-biased rules the rewarded ones use.")]
        public int StartingStarfruits;

        [Tooltip("Up to three. More than that has nowhere to draw.")]
        public List<FruitObjectiveDefinition> Objectives = new();

        public LevelDefinition()
        {
        }

        public LevelDefinition(Vector2Int boardResolution, IEnumerable<TileType> fruits,
            params FruitObjectiveDefinition[] objectives)
            : this(boardResolution, fruits, 0, objectives)
        {
        }

        public LevelDefinition(Vector2Int boardResolution, IEnumerable<TileType> fruits,
            int startingStarfruits, params FruitObjectiveDefinition[] objectives)
        {
            BoardResolution = boardResolution;
            Fruits = new List<TileType>(fruits);
            StartingStarfruits = startingStarfruits;
            Objectives = new List<FruitObjectiveDefinition>(objectives);
        }
    }
}
