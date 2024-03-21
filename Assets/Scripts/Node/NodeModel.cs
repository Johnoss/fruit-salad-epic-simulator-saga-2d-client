using MVC;
using Tile;
using UniRx;
using UnityEngine;

namespace Node
{
    public class NodeModel : AbstractModel
    {
        public Vector2Int Coordinates { get; }
        public RectTransform TileParent { get; private set; }

        private readonly IReactiveProperty<TileContainer> _tile = new ReactiveProperty<TileContainer>();
        public IReadOnlyReactiveProperty<TileContainer> Tile => _tile;

        public NodeModel(Vector2Int coordinates)
        {
            Coordinates = coordinates;
        }

        public void SetTile(TileContainer tile)
        {
            _tile.Value = tile;
        }

        public void SetTileParent(RectTransform tileParent)
        {
            TileParent = tileParent;
        }
    }
}