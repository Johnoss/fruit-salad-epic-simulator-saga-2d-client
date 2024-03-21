using Board;
using Tile;
using UnityEngine;
using Utils;

namespace Node
{
    public class NodeFactory
    {
        [Impject]
        private BoardConfig _boardConfig;

        [Impject]
        private NodeView.ViewFactory _nodeViewFactory;

        [Impject]
        private BoardModel _boardModel;

        [Impject]
        private TileFactory _tileFactory;

        public NodeContainer CreateNode(Vector2Int coordinates)
        {
            var model = new NodeModel(coordinates);
            var controller = new NodeController(model, _boardModel, _boardConfig, _tileFactory);
            _nodeViewFactory.Create(model, controller);

            return new NodeContainer(model, controller);
        }
    }
}