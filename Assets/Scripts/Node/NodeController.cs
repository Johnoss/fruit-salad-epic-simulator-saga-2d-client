using Board;
using MVC;
using Tile;
using UniRx;
using UnityEngine;
using Utils;

namespace Node
{
    public class NodeController : AbstractController
    {
        private readonly NodeModel _model;
        private readonly BoardModel _boardModel;

        private readonly TileFactory _tileFactory;

        public NodeController(NodeModel model, BoardModel boardModel, TileFactory tileFactory)
        {
            _model = model;
            _boardModel = boardModel;
            _tileFactory = tileFactory;

            _boardModel.IsBoardPopulated
                .WhereTrue()
                .SubscribeBlind(CreateRandomTile).AddTo(Disposer);
            
            _model.Tile
                .Skip(1)
                .Where(tile => tile == null)
                .SubscribeBlind(PullAboveTile).AddTo(Disposer);
        }

        private void PullAboveTile()
        {
            // Off the model, not the config: the board is rebuilt at the level's size and the
            // config still holds whatever the first one was.
            for (var x = _model.Coordinates.x; x < _boardModel.GridResolution.Value.x; x++)
            {
                var iterationCoordinate = new Vector2Int(x, _model.Coordinates.y);
                var iterationNode = _boardModel.GetNodeByCoordinates(iterationCoordinate);
                var aboveTile = iterationNode.Model.Tile.Value;
                if (aboveTile == null || aboveTile.Model.IsSelected.Value)
                {
                    continue;
                }
                
                PullTile(iterationNode);
                return;
            }

            CreateRandomTile();
        }

        private void PullTile(NodeContainer aboveNode)
        {
            SetTile(aboveNode.Model.Tile.Value);
            aboveNode.Controller.SetTile(null);
        }

        private void CreateRandomTile()
        {
            var newTile = _tileFactory.CreateRandomTileContainer(_model);
            SetTile(newTile);
        }

        public void SetTile(TileContainer tile)
        {
            tile?.Controller.SetCoordinates(_model.Coordinates);
            _model.SetTile(tile);
        }

        public void SetTileParent(RectTransform tileParent)
        {
            _model.SetTileParent(tileParent);
        }

        public void RemoveTile()
        {
            _model.Tile.Value.Dispose();
            SetTile(null);
        }

        /// <summary>
        /// Swaps this node's tile in place, without the gravity cascade RemoveTile triggers.
        /// The new tile is assigned before the old one is disposed so Tile never goes null,
        /// which is what PullAboveTile listens for.
        /// </summary>
        public void ReplaceTile(TileContainer tile)
        {
            var replacedTile = _model.Tile.Value;

            SetTile(tile);

            replacedTile?.Dispose();
        }
    }
}