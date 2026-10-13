using JetBrains.Annotations;
using MVC;
using UniRx;

namespace Sound
{
    [UsedImplicitly]
    public class SoundModel : AbstractModel
    {
        private readonly IReactiveProperty<bool> _isMutedByPlayer = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsMutedByPlayer => _isMutedByPlayer;

        private readonly IReactiveProperty<bool> _isPlatformAudioEnabled = new ReactiveProperty<bool>(true);
        public IReadOnlyReactiveProperty<bool> IsPlatformAudioEnabled => _isPlatformAudioEnabled;

        /// <summary>
        /// What the player actually hears: the platform's audio state AND the in-game toggle.
        ///
        /// Deliberately a combination rather than a single mute flag. Playables certification
        /// requires the platform's audio state to be the source of truth, so the in-game toggle may
        /// only ever subtract from it - if YouTube says audio is off, no toggle can turn it back on.
        /// </summary>
        public readonly IReadOnlyReactiveProperty<bool> IsAudioEnabled;

        public SoundModel()
        {
            IsAudioEnabled = _isMutedByPlayer
                .CombineLatest(_isPlatformAudioEnabled,
                    (isMutedByPlayer, isPlatformAudioEnabled) => isPlatformAudioEnabled && !isMutedByPlayer)
                .ToReactiveProperty();
        }

        public void SetIsMutedByPlayer(bool isMutedByPlayer)
        {
            _isMutedByPlayer.Value = isMutedByPlayer;
        }

        public void ToggleMutedByPlayer()
        {
            SetIsMutedByPlayer(!_isMutedByPlayer.Value);
        }

        /// <summary>
        /// Where `ytgame.system.onAudioEnabledChange` reports in. Nothing drives this yet, so it
        /// stays true and the toggle is the only thing in play.
        /// </summary>
        public void SetIsPlatformAudioEnabled(bool isPlatformAudioEnabled)
        {
            _isPlatformAudioEnabled.Value = isPlatformAudioEnabled;
        }
    }
}
