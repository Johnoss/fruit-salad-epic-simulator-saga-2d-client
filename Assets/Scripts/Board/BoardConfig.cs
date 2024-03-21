using Node;
using UnityEngine;

namespace Board
{
    [CreateAssetMenu(menuName = "Create BoardConfig", fileName = "BoardConfig", order = 0)]
    public class BoardConfig : ScriptableObject
    {
        [Header("Prefabs")]
        [SerializeField]
        private NodeView _nodePrefab;

        public NodeView NodePrefab => _nodePrefab;

        [Header("Grid")]
        [SerializeField]
        private Vector2Int _gridResolution;
        [SerializeField]
        private int _widthPadding;
        
        public Vector2Int GridResolution => _gridResolution;
        public int WidthPadding => _widthPadding;
        public int GridSize => _gridResolution.x * _gridResolution.y;
    }
}