using MVC;
using UnityEngine;

namespace Tile
{
    public class TileController : AbstractController
    {
        private readonly TileModel _model;

        public TileController(TileModel model)
        {
            _model = model;
        }

        public void SetIsSelected(bool isSelected)
        {
            _model.SetIsSelected(isSelected);
        }

        public void SetCoordinates(Vector2Int coordinates)
        {
            _model.SetCoordinates(coordinates);
        }
    }
}