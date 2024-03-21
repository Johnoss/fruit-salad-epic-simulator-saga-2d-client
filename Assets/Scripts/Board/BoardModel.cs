using JetBrains.Annotations;
using MVC;
using Node;
using Tile;
using UniRx;
using UnityEngine;

namespace Board
{
    [UsedImplicitly]
    public class BoardModel : AbstractModel
    {
        private readonly BoardConfig _boardConfig;
        public NodeContainer[,] Nodes { get; }

        private readonly IReactiveProperty<int> _nodesCount = new ReactiveProperty<int>();

        private readonly IReactiveProperty<float> _nodeSideLength = new ReactiveProperty<float>();
        public IReadOnlyReactiveProperty<float> NodeSideLength => _nodeSideLength;

        public IReadOnlyReactiveProperty<bool> IsBoardPopulated => _nodesCount
            .Select(nodesCount => nodesCount == _boardConfig.GridSize)
            .ToReactiveProperty();

        public BoardModel(BoardConfig boardConfig)
        {
            _boardConfig = boardConfig;
            var gridSize = boardConfig.GridResolution;
            Nodes = new NodeContainer[gridSize.x, gridSize.y];
        }
            
        public void AddNode(NodeContainer node)
        {
            var coordinates = node.Model.Coordinates;
            Nodes[coordinates.x, coordinates.y] = node;
            _nodesCount.Value++;
        }

        public TileContainer GetTileByCoordinates(Vector2Int coordinates)
        {
            return Nodes[coordinates.x, coordinates.y].Model.Tile?.Value;
        }

        public NodeContainer GetNodeByCoordinates(Vector2Int coordinates)
        {
            return Nodes[coordinates.x, coordinates.y];
        }

        public RectTransform GetTileParentByCoordinates(Vector2Int coordinates)
        {
            return Nodes[coordinates.x, coordinates.y].Model.TileParent;
        }

        public void SetNodeSideLength(float length)
        {
            _nodeSideLength.Value = length;
        }
    }
}