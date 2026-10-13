using Sound;
using Zenject;

namespace Installers
{
    public class SoundInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<SoundModel>().AsSingle();

            // NonLazy so the listener volume is applied from the model's initial state even before
            // anything resolves the controller.
            Container.Bind<SoundController>().AsSingle().NonLazy();
        }
    }
}
