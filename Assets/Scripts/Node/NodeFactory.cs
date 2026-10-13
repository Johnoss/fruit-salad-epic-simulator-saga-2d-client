using Board;
using Tile;
using UnityEngine;
using Utils;

namespace Node
{
    public class NodeFactory
    {
        [Impject]
        private NodeView.ViewFactory _nodeViewFactory;

        [Impject]
        private BoardModel _boardModel;

        [Impject]
        private TileFactory _tileFactory;

        public NodeContainer CreateNode(Vector2Int coordinates)
        {
            var model = new NodeModel(coordinates);
            var controller = new NodeController(model, _boardModel, _tileFactory);

            // Kept rather than discarded: the board is rebuilt between levels and the node objects
            // have to be destroyed, which needs a handle on the view.
            var view = _nodeViewFactory.Create(model, controller);

            return new NodeContainer(model, controller, view);
        }
    }
}
