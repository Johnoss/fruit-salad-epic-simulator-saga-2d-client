using Board;
using Level;
using Zenject;

namespace Installers
{
    public class BoardInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<BoardModel>().AsSingle();
            Container.Bind<BoardController>().AsSingle().NonLazy();

            // The level owns the board size, so it is bound alongside the board rather than in an
            // installer of its own - one less thing to drag into the scene. Move it out when the
            // level system grows past this.
            Container.Bind<LevelModel>().AsSingle();

            // NonLazy because nothing asks for it: it is what starts the first level.
            Container.Bind<LevelController>().AsSingle().NonLazy();
        }
    }
}