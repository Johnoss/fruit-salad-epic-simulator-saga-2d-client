using Tile;
using UnityEngine;

namespace Board
{
    [CreateAssetMenu(menuName = "Create LevelConfig", fileName = "LevelConfig", order = 0)]
    public class LevelConfig : ScriptableObject
    {
        [SerializeField] private BoardConfig _boardConfig;
        [SerializeField] private TileConfig _tileConfig;

        public BoardConfig Config => _boardConfig;
        public TileConfig TileConfig => _tileConfig;
    }
}