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
        private readonly IReactiveProperty<Vector2Int> _gridResolution =
            new ReactiveProperty<Vector2Int>(Vector2Int.zero);

        /// <summary>
        /// The board's current dimensions, and the source of truth for them. Read from the level
        /// rather than from BoardConfig, because the board is rebuilt at a new size between levels
        /// and anything still reading the config would be sizing itself against the old board.
        /// </summary>
        public IReadOnlyReactiveProperty<Vector2Int> GridResolution => _gridResolution;

        /// Reallocated on every rebuild, so nothing may hold onto the array itself.
        public NodeContainer[,] Nodes { get; private set; } = new NodeContainer[0, 0];

        private readonly IReactiveProperty<int> _nodesCount = new ReactiveProperty<int>();

        private readonly IReactiveProperty<float> _nodeSideLength = new ReactiveProperty<float>();
        public IReadOnlyReactiveProperty<float> NodeSideLength => _nodeSideLength;

        /// <summary>
        /// True once every square of the current board has a node on it. Nodes create their first
        /// tile off this, so it has to fall to false while a board is being replaced - hence the
        /// zero check, which is what a half-built board looks like.
        /// </summary>
        public readonly IReadOnlyReactiveProperty<bool> IsBoardPopulated;

        public BoardModel()
        {
            // Built once rather than per access. As an expression-bodied property this handed every
            // caller its own property with its own subscription, and there is one caller per node.
            IsBoardPopulated = _nodesCount
                .CombineLatest(_gridResolution,
                    (nodesCount, gridResolution) =>
                        nodesCount > 0 && nodesCount == gridResolution.x * gridResolution.y)
                .ToReactiveProperty();
        }

        /// <summary>
        /// Empties the board and resizes it. The caller disposes the outgoing nodes first - this
        /// only drops the model's handle on them.
        ///
        /// The count is zeroed before the resolution changes, so IsBoardPopulated goes false on the
        /// way through. Resizing first could land on a size the stale count happens to match, and
        /// the nodes would never see the board empty.
        /// </summary>
        public void RebuildNodes(Vector2Int gridResolution)
        {
            _nodesCount.Value = 0;
            _gridResolution.Value = gridResolution;

            Nodes = new NodeContainer[Mathf.Max(0, gridResolution.x), Mathf.Max(0, gridResolution.y)];
        }

        public void AddNode(NodeContainer node)
        {
            var coordinates = node.Model.Coordinates;
            Nodes[coordinates.x, coordinates.y] = node;
            _nodesCount.Value++;
        }

        public bool IsWithinGrid(Vector2Int coordinates)
        {
            return coordinates.x >= 0 && coordinates.x < _gridResolution.Value.x
                                      && coordinates.y >= 0 && coordinates.y < _gridResolution.Value.y;
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
