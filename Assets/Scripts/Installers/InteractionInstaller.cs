using Interaction;
using Tile;
using Utils;
using Zenject;

namespace Installers
{
    public class InteractionInstaller : MonoInstaller
    {
        [Impject]
        private TileConfig _tileConfig;

        public override void InstallBindings()
        {
            Container.Bind<SelectionModel>().AsSingle();
            Container.Bind<SelectionController>().AsSingle();
            
            Container.Bind<InputModel>().AsSingle();
            Container.Bind<InputController>().AsSingle().NonLazy();
        }
    }
}