using Boosters;
using Platform;
using Save;
using YTGameSDK;
using UnityEngine;
using Zenject;

namespace Installers
{
    public class MonetizationInstaller : MonoInstaller
    {
        [Header("Config")]
        [SerializeField]
        private BoosterConfig _boosterConfig;

        [Header("Playables SDK")]
        [SerializeField]
        [Tooltip("The YTGameWrapper component. Its GameObject must be named exactly 'YTGameWrapper' " +
                 "- the jslib bridge sends messages to that name.")]
        private YTGameWrapper _ytGameWrapper;

        [Header("Health reporting")]
        [SerializeField]
        [Tooltip("Also forward warnings to the platform health page. Off by default - the API is " +
                 "rate-limited, and warnings crowd out the errors worth seeing.")]
        private bool _reportWarningsToPlatform;

        [Header("Non-Playables stub")]
        [SerializeField]
        [Tooltip("Seconds the stubbed rewarded ad takes to resolve")]
        private float _simulatedAdSeconds = 1f;

        [SerializeField]
        [Tooltip("Return NoFill instead of Granted, to exercise the graceful degradation path. " +
                 "Certification requires the game stays fully playable when no ad is served.")]
        private bool _simulateNoFill;

        public override void InstallBindings()
        {
            Container.BindInstance(_boosterConfig);

            if (GetIsPlayablesEnvironment())
            {
                Container.Bind<IPlatformServices>().To<YTGamePlatform>().AsSingle()
                    .WithArguments(_ytGameWrapper);
            }
            else
            {
                Container.Bind<IPlatformServices>().To<EditorStubPlatform>().AsSingle()
                    .WithArguments(_simulatedAdSeconds, _simulateNoFill);
            }

            // Lifecycle lives here because this is where IPlatformServices is bound; the installer
            // is effectively the platform installer as well as the monetization one.
            Container.Bind<GameLifecycleController>().AsSingle().NonLazy();

            Container.Bind<PlatformHealthReporter>().AsSingle()
                .WithArguments(_reportWarningsToPlatform).NonLazy();

            Container.Bind<SaveModel>().AsSingle();
            Container.Bind<SaveController>().AsSingle().NonLazy();

            Container.Bind<BoosterModel>().AsSingle();
            Container.Bind<StarfruitPlacer>().AsSingle();
            Container.Bind<BoosterController>().AsSingle().NonLazy();
        }

        /// <summary>
        /// Deliberately not just the wrapper's own InPlayablesEnv(): that returns true in the
        /// editor, where there is no SDK to talk to. The compile-time guard is what actually keeps
        /// the editor and any non-Playables WebGL build on the stub.
        /// </summary>
        private bool GetIsPlayablesEnvironment()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return _ytGameWrapper != null && _ytGameWrapper.InPlayablesEnv();
#else
            return false;
#endif
        }
    }
}
