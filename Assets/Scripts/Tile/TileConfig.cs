using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tile
{
    [CreateAssetMenu(menuName = "Create TileConfig", fileName = "TileConfig", order = 0)]
    public class TileConfig : ScriptableObject
    {
        [Header("Prefabs")]
        [SerializeField]
        private TileView _tilePrefab;

        public TileView TilePrefab => _tilePrefab;
        
        [Header("Tiles")]
        [SerializeField]
        [FormerlySerializedAs("_TileSettings")]
        private List<TileSetting> _tileSettings;
        
        public List<TileSetting> TileSettings => _tileSettings;

        public Sprite GetTileSpriteOrDefault(TileType type)
        {
            return _tileSettings.FirstOrDefault(setting => setting.TileType == type)?.Sprite;
        }

        public Color GetSpriteColor(TileType type)
        {
            return _tileSettings.FirstOrDefault(setting => setting.TileType == type)?.Color ?? Color.clear;
        }
    }
}