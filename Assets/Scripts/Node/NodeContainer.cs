using System;

namespace Node
{
    public class NodeContainer : IDisposable
    {
        public NodeModel Model { get; }
        public NodeController Controller { get; }
        public NodeView View { get; }

        public NodeContainer(NodeModel model, NodeController controller, NodeView view)
        {
            Model = model;
            Controller = controller;
            View = view;
        }

        /// <summary>
        /// Tears the node down for a board rebuild.
        ///
        /// Order matters. The controller is disposed first because it watches its own tile going
        /// away and answers by pulling the tile above down - on a board being deleted that is work
        /// against nodes that are already gone. The tile follows, which returns its view to the
        /// pool rather than destroying it, so a new board of any size refills from what the old one
        /// left behind.
        /// </summary>
        public void Dispose()
        {
            Controller.Dispose();

            Model.Tile.Value?.Dispose();
            Model.Dispose();

            View.Dispose();
        }
    }
}
