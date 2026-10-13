using System;
using JetBrains.Annotations;
using UniRx;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Stand-in for the real SDK so the game runs outside the Playables harness.
    ///
    /// Simulates the latency of a rewarded ad and can be told to return NoFill, which is the path
    /// that has to stay playable and is otherwise awkward to reproduce on demand. Saves go to
    /// PlayerPrefs, which is local-only and deliberately not what ships.
    /// </summary>
    [UsedImplicitly]
    public class EditorStubPlatform : IPlatformServices
    {
        private const string SaveKey = "StubPlatformSave";

        private readonly float _simulatedAdSeconds;
        private readonly bool _simulateNoFill;

        private readonly Subject<bool> _audioEnabledChanged = new();
        private readonly Subject<Unit> _paused = new();
        private readonly Subject<Unit> _resumed = new();

        public EditorStubPlatform(float simulatedAdSeconds, bool simulateNoFill)
        {
            _simulatedAdSeconds = simulatedAdSeconds;
            _simulateNoFill = simulateNoFill;
        }

        public bool IsPlayablesEnvironment => false;

        /// Always on outside Playables; there is no platform mute to respect.
        public bool IsAudioEnabled => true;

        public IObservable<bool> AudioEnabledChanged => _audioEnabledChanged;

        public IObservable<Unit> Paused => _paused;

        public IObservable<Unit> Resumed => _resumed;

        public void NotifyFirstFrameReady()
        {
            Debug.Log("[EditorStubPlatform] firstFrameReady");
        }

        public void NotifyGameReady()
        {
            Debug.Log("[EditorStubPlatform] gameReady");
        }

        public IObservable<AdResult> ShowRewardedAd(string rewardId)
        {
            Debug.Log($"[EditorStubPlatform] Rewarded ad requested: '{rewardId}'");

            // IgnoreTimeScale so the stub keeps resolving if the game is ever paused via timeScale.
            return Observable
                .Timer(TimeSpan.FromSeconds(_simulatedAdSeconds), Scheduler.MainThreadIgnoreTimeScale)
                .Select(_ => _simulateNoFill ? AdResult.NoFill : AdResult.Granted)
                .Do(result => Debug.Log($"[EditorStubPlatform] Rewarded ad '{rewardId}' resolved: {result}"));
        }

        public bool ShowInterstitialAd()
        {
            // Nothing to simulate - there is no interstitial callback to fake and no game state
            // that depends on the outcome. The log line is the whole point: it is how the cadence
            // gets checked without a Playables build.
            Debug.Log($"[EditorStubPlatform] Interstitial ad requested, accepted: {!_simulateNoFill}");

            return !_simulateNoFill;
        }

        public bool SaveData(string data)
        {
            PlayerPrefs.SetString(SaveKey, data);
            PlayerPrefs.Save();

            return true;
        }

        public IObservable<string> LoadData()
        {
            // Deferred by a frame so callers see the same asynchronous shape the real SDK has,
            // rather than working by accident because the stub answered synchronously.
            return Observable
                .NextFrame()
                .Select(_ => PlayerPrefs.GetString(SaveKey, string.Empty));
        }

        public void SendScore(int score)
        {
            Debug.Log($"[EditorStubPlatform] sendScore {score}");
        }

        public void LogError(string message)
        {
            Debug.LogError($"[EditorStubPlatform] logError: {message}");
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning($"[EditorStubPlatform] logWarning: {message}");
        }
    }
}
