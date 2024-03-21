using System.Linq;
using Board;
using JetBrains.Annotations;
using MVC;
using Score;
using Tile;
using UniRx;
using UnityEngine;
using Utils;

namespace Interaction
{
    [UsedImplicitly]
    public class SelectionController : AbstractController
    {
        private readonly SelectionModel _model;
        private readonly BoardModel _boardModel;
        
        private readonly ScoreController _scoreController;

        private readonly ScoreConfig _scoreConfig;

        public SelectionController(SelectionModel model, BoardModel boardModel, InputModel inputModel,
            ScoreController scoreController, ScoreConfig scoreConfig)
        {
            _model = model;
            _boardModel = boardModel;
            _scoreController = scoreController;
            _scoreConfig = scoreConfig;

            inputModel.IsInteracting
                .WhereFalse()
                .SubscribeBlind(ResolveSelection).AddTo(Disposer);
        }

        public void TrySelectTile(Vector2Int coordinates)
        {
            var tile = _boardModel.GetTileByCoordinates(coordinates);
            var tileModel = tile.Model;
            
            if (!GetIsValidSelectionType(tile.TileType))
            {
                return;
            }

            if (tileModel.IsSelected.Value)
            {
                DeselectPreviousUntil(tileModel);

                return;
            }
            
            if (_model.SelectedTiles.Any() && !GetIsAdjacentToLastSelected(tileModel))
            {
                return;
            }
            
            _model.SetSelectedType(tile.TileType);
            
            tile.Controller.SetIsSelected(true);
            _model.AddSelectedTile(tile);
        }

        private void ResolveSelection()
        {
            var chainLength = _model.SelectedTiles.Count;
            
            if (chainLength >= _scoreConfig.MinChain)
            {
                _model.Collect(chainLength);
                _scoreController.AddCollectedChainScore(chainLength);
                RemoveSelected();
            }
            
            DeselectPreviousUntil();
            _model.ClearSelectedType();
        }

        private void RemoveSelected()
        {
            foreach (var selectedTile in _model.SelectedTiles)
            {
                _boardModel.GetNodeByCoordinates(selectedTile.Model.Coordinates.Value).Controller.RemoveTile();
            }
        }

        private void DeselectPreviousUntil(TileModel tileModel = null)
        {
            for (var i = _model.SelectedTiles.Count - 1; i >= 0; i--)
            {
                var iteratedTile = _model.SelectedTiles[i];
                if (iteratedTile.Model == tileModel)
                {
                    break;
                }

                iteratedTile.Controller.SetIsSelected(false);
                _model.RemoveSelectedTile(i);
            }
        }

        private bool GetIsAdjacentToLastSelected(TileModel tileModel)
        {
            return tileModel.Coordinates.Value.IsAdjacentTo(_model.SelectedTiles.Last().Model.Coordinates.Value);
        }

        private bool GetIsValidSelectionType(TileType attemptedType)
        {
            var selectedType = _model.SelectedTileType.Value;
            return selectedType == TileType.None || attemptedType == selectedType;
        }
    }
}