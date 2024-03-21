namespace Node
{
    public class NodeContainer
    {
        public NodeModel Model { get; }
        public NodeController Controller { get; }

        public NodeContainer(NodeModel model, NodeController controller)
        {
            Model = model;
            Controller = controller;
        }
    }
}