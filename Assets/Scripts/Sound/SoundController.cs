using JetBrains.Annotations;
using MVC;
using Platform;
using UniRx;
using UnityEngine;
using Zenject;

namespace Sound
{
    [UsedImplicitly]
    public class SoundController : AbstractController
    {
        private readonly SoundModel _model;

        public SoundController(SoundModel model, [InjectOptional] IPlatformServices platformServices)
        {
            _model = model;

            _model.IsAudioEnabled.Subscribe(ApplyAudioEnabled).AddTo(Disposer);

            if (platformServices == null)
            {
                return;
            }

            // Seeded from the platform and then followed. Certification is explicit that the
            // platform's audio state is the source of truth, so it is read at startup rather than
            // assuming audio begins enabled, and the in-game toggle can only subtract from it.
            _model.SetIsPlatformAudioEnabled(platformServices.IsAudioEnabled);

            platformServices.AudioEnabledChanged
                .Subscribe(_model.SetIsPlatformAudioEnabled)
                .AddTo(Disposer);
        }

        public void ToggleMute()
        {
            _model.ToggleMutedByPlayer();
        }

        private static void ApplyAudioEnabled(bool isAudioEnabled)
        {
            // Effects and music in one switch. Every source in the scene feeds the single listener,
            // so this covers the music source, both one-shot channels and the intro sting together,
            // without having to know about any of them.
            //
            // Volume rather than AudioListener.pause: pausing would freeze the music mid-bar and
            // resume it there, where muting lets it keep running and come back in place.
            AudioListener.volume = isAudioEnabled ? 1f : 0f;
        }
    }
}
