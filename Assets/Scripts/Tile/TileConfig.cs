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

        private List<TileSetting> _spawnableTileSettings;
        private TileSetting _wildcardTileSetting;

        /// <summary>
        /// The pool the board refills from. Wildcards are excluded - they are paid for, so letting
        /// one fall in for free would undercut the booster.
        /// </summary>
        public List<TileSetting> SpawnableTileSettings =>
            _spawnableTileSettings ??= _tileSettings.Where(setting => !setting.IsWildcard).ToList();

        /// Null if no tile setting is flagged IsWildcard, in which case the Starfruit booster is inert.
        public TileSetting WildcardTileSetting =>
            _wildcardTileSetting ??= _tileSettings.FirstOrDefault(setting => setting.IsWildcard);

        public Sprite GetTileSpriteOrDefault(TileType type)
        {
            return _tileSettings.FirstOrDefault(setting => setting.TileType == type)?.Sprite;
        }

        public Color GetSpriteColor(TileType type)
        {
            return _tileSettings.FirstOrDefault(setting => setting.TileType == type)?.Color ?? Color.clear;
        }

        private void OnValidate()
        {
            // Editing the list in the inspector must not leave the cached views stale.
            _spawnableTileSettings = null;
            _wildcardTileSetting = null;
        }
    }
}
