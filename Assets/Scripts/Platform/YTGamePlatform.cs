using System;
using JetBrains.Annotations;
using UniRx;
using UnityEngine;
using YTGameSDK;

namespace Platform
{
    /// <summary>
    /// IPlatformServices on top of Google's experimental Unity wrapper.
    ///
    /// The wrapper is callback-based and keeps a single stored delegate per event, so everything
    /// here converts to observables once, at construction, and shares them. Registering a callback
    /// twice would silently replace the first one.
    /// </summary>
    [UsedImplicitly]
    public class YTGamePlatform : IPlatformServices
    {
        private readonly YTGameWrapper _wrapper;

        private readonly Subject<bool> _audioEnabledChanged = new();
        private readonly Subject<Unit> _paused = new();
        private readonly Subject<Unit> _resumed = new();

        public YTGamePlatform(YTGameWrapper wrapper)
        {
            _wrapper = wrapper;

            // Registered once here rather than per subscriber: the wrapper holds one delegate per
            // event, so a second registration would drop the first.
            _wrapper.SetOnAudioEnabledChangeCallback(isAudioEnabled => _audioEnabledChanged.OnNext(isAudioEnabled));
            _wrapper.SetOnPauseCallback(() => _paused.OnNext(Unit.Default));
            _wrapper.SetOnResumeCallback(() => _resumed.OnNext(Unit.Default));
        }

        public bool IsPlayablesEnvironment => _wrapper.InPlayablesEnv();

        public bool IsAudioEnabled => _wrapper.IsYTGameAudioEnabled();

        public IObservable<bool> AudioEnabledChanged => _audioEnabledChanged;

        public IObservable<Unit> Paused => _paused;

        public IObservable<Unit> Resumed => _resumed;

        public void NotifyFirstFrameReady()
        {
            _wrapper.SendGameFirstFrameReady();
        }

        public void NotifyGameReady()
        {
            _wrapper.SendGameIsReady();
        }

        public IObservable<AdResult> ShowRewardedAd(string rewardId)
        {
            return Observable.Create<AdResult>(observer =>
            {
                // The wrapper reports a single bool. There is no way to tell a dismissal from an
                // unfilled request, and callers treat both the same, so it collapses to NoFill.
                _wrapper.RequestRewardedAd(rewardId, isRewardEarned =>
                {
                    observer.OnNext(isRewardEarned ? AdResult.Granted : AdResult.NoFill);
                    observer.OnCompleted();
                });

                return Disposable.Empty;
            });
        }

        public bool ShowInterstitialAd()
        {
            // Zero means accepted; anything else means the platform declined, which is usually just
            // ad pacing. The wrapper has already logged the code.
            return _wrapper.RequestInterstitialAd() == 0;
        }

        public bool SaveData(string data)
        {
            // Zero means accepted; anything else is a failure the wrapper has already logged.
            return _wrapper.SendGameSaveData(data) == 0;
        }

        public IObservable<string> LoadData()
        {
            return Observable.Create<string>(observer =>
            {
                _wrapper.LoadGameSaveData(data =>
                {
                    observer.OnNext(data ?? string.Empty);
                    observer.OnCompleted();
                });

                return Disposable.Empty;
            });
        }

        public void SendScore(int score)
        {
            _wrapper.SendGameScore(score);
        }

        public void LogError(string message)
        {
            _wrapper.SendYTGameError(message);
        }

        public void LogWarning(string message)
        {
            _wrapper.SendYTGameWarning(message);
        }
    }
}
