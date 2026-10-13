using System;
using UnityEngine;

namespace Tile
{
    [Serializable]
    public class TileSetting
    {
        public TileType TileType;
        public Sprite Sprite;
        public Color Color;

        [Tooltip("Matches any other tile. Wildcards are never spawned by the board - they only " +
                 "arrive from a booster - so this also excludes the tile from the random refill pool.")]
        public bool IsWildcard;
    }
}
