using System;
using UniRx;

namespace Platform
{
    /// <summary>
    /// Abstraction over the publisher SDK. Game code talks to this and never to YTGameSDK types
    /// directly, so switching publisher (Playgama, GamePush) touches only the implementation.
    ///
    /// Shaped after Google's actual Unity wrapper rather than the documented JS API, because the
    /// two do not line up: the wrapper returns save status synchronously, hands back rewarded ads
    /// as a bool through a single stored callback, and takes score as an int.
    /// </summary>
    public interface IPlatformServices
    {
        /// <summary>
        /// Whether the game is really running inside Playables. Note the wrapper's own
        /// InPlayablesEnv() answers true in the editor, so this must not be the only thing
        /// deciding which implementation is used.
        /// </summary>
        bool IsPlayablesEnvironment { get; }

        /// Call once the loading screen is on screen and visibly communicating loading.
        void NotifyFirstFrameReady();

        /// Call only when the game is genuinely interactable. YouTube's spinner stays up until this.
        void NotifyGameReady();

        /// <summary>
        /// Player-initiated rewarded ad. Always completes with exactly one AdResult and never
        /// faults, so a missing ad is an outcome rather than an error to handle.
        /// </summary>
        IObservable<AdResult> ShowRewardedAd(string rewardId);

        /// <summary>
        /// Fire and forget. Returns whether the SDK accepted the request, which is not the same as
        /// the player having watched anything - the wrapper reports a status synchronously and has
        /// no completion callback for interstitials, so there is nothing to wait on and no way to
        /// find out how it ended.
        ///
        /// A refusal is normal: the platform rate-limits these, so a rejected request means it is
        /// too soon, not that anything is wrong. Callers must carry on either way.
        /// </summary>
        bool ShowInterstitialAd();

        /// Returns false if the SDK rejected the save.
        bool SaveData(string data);

        /// Completes with the stored payload, or an empty string when there is nothing saved.
        IObservable<string> LoadData();

        /// The best score sent must always match the best score in the save; certification checks it.
        void SendScore(int score);

        /// The platform's audio state, which is the source of truth. Never our own mute flag.
        bool IsAudioEnabled { get; }

        IObservable<bool> AudioEnabledChanged { get; }

        /// YouTube gives only a short window after this before the game may be evicted.
        IObservable<Unit> Paused { get; }

        IObservable<Unit> Resumed { get; }

        /// The only crash telemetry the platform offers. Best-effort and rate-limited.
        void LogError(string message);

        void LogWarning(string message);
    }
}
