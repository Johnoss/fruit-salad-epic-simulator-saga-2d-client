using Board;
using Node;
using UnityEngine;
using Utils;
using Zenject;

namespace Installers
{
    public class NodeInstaller : MonoInstaller
    {
        [Impject]
        private BoardConfig _boardConfig;

        [SerializeField]
        private Transform _nodesParent;

        public override void InstallBindings()
        {
            Container.Bind<NodeFactory>().AsSingle();
            
            Container
                .BindFactory<NodeModel, NodeController, NodeView, NodeView.ViewFactory>()
                .FromComponentInNewPrefab(_boardConfig.NodePrefab)
                .UnderTransform(_nodesParent);
        }
    }
}