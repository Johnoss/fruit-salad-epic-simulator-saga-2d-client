using Board;
using Score;
using Sound;
using Tile;
using UnityEngine;
using Zenject;

namespace Installers
{
    [CreateAssetMenu(fileName = "ConfigInstaller", menuName = "Installers/ConfigInstaller")]
    public class ConfigInstaller : ScriptableObjectInstaller<ConfigInstaller>
    {
        [SerializeField]
        private ScoreConfig _scoreConfig;
        [SerializeField]
        private BoardConfig _boardConfig;
        [SerializeField]
        private TileConfig _tileConfig;
        [SerializeField]
        private SoundConfig _soundConfig;
        [SerializeField]
        private GoalConfig _goalConfig;
        
        public override void InstallBindings()
        {
            Container.BindInstance(_scoreConfig);
            Container.BindInstance(_boardConfig);
            Container.BindInstance(_tileConfig);
            Container.BindInstance(_soundConfig);
            Container.BindInstance(_goalConfig);
        }
    }
}