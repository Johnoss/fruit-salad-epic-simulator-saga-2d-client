using Board;
using Zenject;

namespace Installers
{
    public class BoardInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<BoardModel>().AsSingle();
            Container.Bind<BoardController>().AsSingle().NonLazy();
        }
    }
}