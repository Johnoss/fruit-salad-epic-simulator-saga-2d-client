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
        
        private readonly BoardConfig _boardConfig;

        private readonly TileFactory _tileFactory;

        public NodeController(NodeModel model, BoardModel boardModel, BoardConfig boardConfig, TileFactory tileFactory)
        {
            _model = model;
            _boardModel = boardModel;
            _boardConfig = boardConfig;
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
            for (var x = _model.Coordinates.x; x < _boardConfig.GridResolution.x; x++)
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
    }
}