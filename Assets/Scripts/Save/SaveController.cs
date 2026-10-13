using System;
using JetBrains.Annotations;
using MVC;
using Platform;
using UniRx;
using UnityEngine;
using Utils;

namespace Save
{
    [UsedImplicitly]
    public class SaveController : AbstractController
    {
        /// <summary>
        /// Collapses a burst of changes into one write. A single chain can raise the high score and
        /// complete a fruit goal in the same frame, and the platform save is rate-limited.
        /// </summary>
        private const float SaveDebounceSeconds = 0.5f;

        private readonly SaveModel _model;
        private readonly IPlatformServices _platformServices;

        private bool _hasWarnedAboutNewerBuild;

        public SaveController(SaveModel model, IPlatformServices platformServices)
        {
            _model = model;
            _platformServices = platformServices;

            _platformServices.LoadData()
                .Subscribe(ApplyLoaded, OnLoadFailed)
                .AddTo(Disposer);

            // Unscaled, so a pending save is not stranded by a pause setting timeScale to zero.
            _model.Changed
                .Throttle(TimeSpan.FromSeconds(SaveDebounceSeconds), Scheduler.MainThreadIgnoreTimeScale)
                .SubscribeBlind(Save)
                .AddTo(Disposer);

            // YouTube gives only a short window after onPause before the game may be evicted, so
            // this one skips the debounce entirely.
            _platformServices.Paused
                .SubscribeBlind(Save)
                .AddTo(Disposer);

            // Sending the score off the same field the save holds is what keeps the two consistent.
            // Certification checks that the best score sent matches the best score in the save, and
            // deriving both from one value makes disagreeing impossible rather than unlikely.
            _model.Changed.SubscribeBlind(SendGrandTotal).AddTo(Disposer);
            _model.IsLoaded.WhereTrue().SubscribeBlind(SendGrandTotal).AddTo(Disposer);
        }

        private void ApplyLoaded(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                // No save yet. A first-time player, not a failure.
                _model.ApplyLoaded(new SaveData(), false);
                return;
            }

            SaveData loaded;

            try
            {
                loaded = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception exception)
            {
                // Corrupt payload. Starting fresh loses this player's progress, so it is worth
                // telling the platform about rather than swallowing.
                _platformServices.LogError($"Save parse failed: {exception.Message}");
                _model.ApplyLoaded(new SaveData(), false);
                return;
            }

            if (loaded == null)
            {
                _model.ApplyLoaded(new SaveData(), false);
                return;
            }

            var isFromNewerBuild = loaded.SchemaVersion > SaveData.CurrentSchemaVersion;

            if (isFromNewerBuild)
            {
                // Read it, use it, never write over it. Fields this build does not know about are
                // already lost from the in-memory copy, so saving would destroy them for good.
                Debug.LogWarning($"[Save] Payload is schema {loaded.SchemaVersion}, newer than this " +
                                 $"build's {SaveData.CurrentSchemaVersion}. Running read-only to avoid " +
                                 "overwriting newer data.");

                _model.ApplyLoaded(loaded, true);
                return;
            }

            _model.ApplyLoaded(SaveMigrator.Migrate(loaded), false);
        }

        private void OnLoadFailed(Exception exception)
        {
            _platformServices.LogError($"Save load failed: {exception.Message}");

            // Still mark loaded, or the game waits forever on a save that is never coming.
            _model.ApplyLoaded(new SaveData(), false);
        }

        private void Save()
        {
            if (_model.IsFromNewerBuild)
            {
                if (!_hasWarnedAboutNewerBuild)
                {
                    _hasWarnedAboutNewerBuild = true;
                    Debug.LogWarning("[Save] Skipping save: stored data came from a newer build.");
                }

                return;
            }

            if (!_platformServices.SaveData(JsonUtility.ToJson(_model.Data)))
            {
                _platformServices.LogError("Save rejected by the platform.");
            }
        }

        private void SendGrandTotal()
        {
            // Clamped because the score is a float and the platform takes an int; the end-of-content
            // threshold alone sits at a billion.
            var grandTotal = Mathf.Clamp(_model.Data.GrandTotalScore, 0f, int.MaxValue);

            _platformServices.SendScore(Mathf.RoundToInt(grandTotal));
        }
    }
}
