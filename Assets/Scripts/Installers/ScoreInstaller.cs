using Score;
using Zenject;

namespace Installers
{
    public class ScoreInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<ScoreModel>().AsSingle();
            Container.Bind<ScoreController>().AsSingle();
            
            Container.Bind<GoalModel>().AsSingle();
            Container.Bind<GoalController>().AsSingle().NonLazy();
        }
    }
}