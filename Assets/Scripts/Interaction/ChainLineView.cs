using System.Linq;
using Board;
using MVC;
using Tile;
using UniRx;
using UnityEngine;
using Utils;
using Zenject;

namespace Interaction
{
    public class ChainLineView : AbstractView, IInitializable
    {
        [Header("Rendering")]
        [SerializeField]
        private LineRenderer _lineRenderer;
        
        [Impject]
        private SelectionModel _selectionModel;
        [Impject]
        private BoardModel _boardModel;

        [Impject]
        private TileConfig _tileConfig;
        
        [Impject]
        public void Initialize()
        {
            _selectionModel.SelectedTiles
                .ObserveCountChanged()
                .SubscribeBlind(UpdateLine).AddTo(Disposer);

            _selectionModel.SelectedTileType.Subscribe(ChangeLineColor).AddTo(Disposer);
        }

        private void ChangeLineColor(TileType type)
        {
            _lineRenderer.endColor = _tileConfig.GetSpriteColor(type);
        }

        private void UpdateLine()
        {
            var selectedTilesPositions = _selectionModel.SelectedTiles.Select(tile =>
                _boardModel.GetTileParentByCoordinates(tile.Model.Coordinates.Value).position).ToArray();

            _lineRenderer.positionCount = selectedTilesPositions.Length;
            _lineRenderer.SetPositions(selectedTilesPositions);
        }
    }
}