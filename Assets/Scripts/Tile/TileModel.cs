using MVC;
using UniRx;
using UnityEngine;

namespace Tile
{
    public class TileModel : AbstractModel
    {
        public TileSetting TileSetting { get; }

        private readonly IReactiveProperty<Vector2Int> _coordinates = new ReactiveProperty<Vector2Int>();
        public IReadOnlyReactiveProperty<Vector2Int> Coordinates => _coordinates;
        
        private readonly IReactiveProperty<bool> _isSelected = new ReactiveProperty<bool>();
        public IReadOnlyReactiveProperty<bool> IsSelected => _isSelected;

        public TileModel(TileSetting tileSetting, Vector2Int coordinates)
        {
            TileSetting = tileSetting;

            _coordinates.Value = coordinates;
        }

        public void SetIsSelected(bool isSelected)
        {
            _isSelected.Value = isSelected;
        }

        public void SetCoordinates(Vector2Int coordinates)
        {
            _coordinates.Value = coordinates;
        }
    }
}