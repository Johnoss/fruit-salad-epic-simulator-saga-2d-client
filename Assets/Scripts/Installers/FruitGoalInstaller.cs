using FruitGoal;
using UnityEngine;
using Zenject;

namespace Installers
{
    public class FruitGoalInstaller : MonoInstaller
    {
        [SerializeField]
        private FruitGoalConfig _fruitGoalConfig;

        public override void InstallBindings()
        {
            Container.BindInstance(_fruitGoalConfig);

            Container.Bind<FruitGoalModel>().AsSingle();

            // NonLazy so the level's objectives are built even before the panel asks for them.
            Container.Bind<FruitGoalController>().AsSingle().NonLazy();
        }
    }
}
