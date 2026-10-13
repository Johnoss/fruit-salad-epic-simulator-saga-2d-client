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
            
            // Backing out is checked before validity, not after. Now that a chain can change type
            // partway through, an already-selected tile can legitimately fail the match test -
            // in [apple, starfruit, orange] the apple no longer matches the chain's current type -
            // and retreating onto it must never be blocked by that.
            if (tileModel.IsSelected.Value)
            {
                DeselectPreviousUntil(tileModel);

                return;
            }

            if (!GetIsValidSelection(tile))
            {
                return;
            }

            if (_model.SelectedTiles.Any() && !GetIsAdjacentToLastSelected(tileModel))
            {
                return;
            }

            // A wildcard never sets the type, so it leaves the chain open. Every real fruit does
            // set it, which is what makes the chain's type the most recent fruit rather than the
            // first one.
            if (!tile.IsWildcard)
            {
                _model.SetSelectedType(tile.TileType);
            }

            tile.Controller.SetIsSelected(true);
            _model.AddSelectedTile(tile);
        }

        /// <summary>
        /// Drops the tile at these coordinates out of the current chain, along with everything
        /// selected after it.
        ///
        /// For when something replaces a tile the player is mid-chain on. Without it the selection
        /// keeps holding a disposed TileContainer, and releasing the chain would remove whatever
        /// took the tile's place on that node instead - so a Starfruit landing on a chained tile
        /// would be eaten by the very chain it landed in.
        /// </summary>
        public void DropSelectionAt(Vector2Int coordinates)
        {
            for (var i = _model.SelectedTiles.Count - 1; i >= 0; i--)
            {
                var selectedTile = _model.SelectedTiles[i];
                var isDroppedTile = selectedTile.Model.Coordinates.Value == coordinates;

                selectedTile.Controller.SetIsSelected(false);
                _model.RemoveSelectedTile(i);

                if (isDroppedTile)
                {
                    break;
                }
            }

            RefreshSelectedType();
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

            RefreshSelectedType();
        }

        /// <summary>
        /// Re-derives the chain's type from what is still selected, rather than leaving whatever
        /// the last added tile happened to set.
        ///
        /// The type is the *most recent* remaining fruit, matching how the chain builds forwards:
        /// a chain that switched apple into orange through a Starfruit is an orange chain, and
        /// retreating back past the orange makes it an apple chain again. Retreating to nothing but
        /// Starfruits leaves no fruit to define it, so the chain reopens to any type.
        /// </summary>
        private void RefreshSelectedType()
        {
            var typeDefiningTile = _model.SelectedTiles.LastOrDefault(selectedTile => !selectedTile.IsWildcard);

            _model.SetSelectedType(typeDefiningTile?.TileType ?? TileType.None);
        }

        private bool GetIsAdjacentToLastSelected(TileModel tileModel)
        {
            return tileModel.Coordinates.Value.IsAdjacentTo(_model.SelectedTiles.Last().Model.Coordinates.Value);
        }

        private bool GetIsValidSelection(TileContainer attemptedTile)
        {
            if (attemptedTile.IsWildcard)
            {
                return true;
            }

            // A Starfruit at the head of the chain bridges into any fruit, so a chain can switch
            // type through it - apple into orange in one connect. Only the tile immediately after
            // the wildcard is free; once a real fruit lands, the chain is locked to it again.
            var lastSelectedTile = _model.SelectedTiles.LastOrDefault();
            if (lastSelectedTile != null && lastSelectedTile.IsWildcard)
            {
                return true;
            }

            var selectedType = _model.SelectedTileType.Value;
            return selectedType == TileType.None || attemptedTile.TileType == selectedType;
        }
    }
}