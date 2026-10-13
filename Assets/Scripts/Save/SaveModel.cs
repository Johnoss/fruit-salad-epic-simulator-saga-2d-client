using System;
using JetBrains.Annotations;
using MVC;
using UniRx;

namespace Save
{
    [UsedImplicitly]
    public class SaveModel : AbstractModel
    {
        private SaveData _data = new();

        public SaveData Data => _data;

        private readonly Subject<Unit> _changed = new();

        /// Fires whenever the payload actually changed. The controller persists off this, so any
        /// new piece of saved state gets saved without having to find every call site.
        public IObservable<Unit> Changed => _changed;

        private readonly IReactiveProperty<bool> _isLoaded = new ReactiveProperty<bool>();
        public IReadOnlyReactiveProperty<bool> IsLoaded => _isLoaded;

        /// <summary>
        /// Set when the stored payload came from a newer build than this one. Saving is then
        /// refused outright: certification requires never overwriting newer data with older, and a
        /// player who ran a newer version elsewhere must not lose it by opening this one.
        /// </summary>
        public bool IsFromNewerBuild { get; private set; }

        public void ApplyLoaded(SaveData loadedData, bool isFromNewerBuild)
        {
            IsFromNewerBuild = isFromNewerBuild;

            // Merged rather than assigned. A run can finish before the load returns, and taking the
            // higher value means a score set in that window survives instead of being overwritten
            // by the older stored one.
            var highestGrandTotal = loadedData.GrandTotalScore > _data.GrandTotalScore
                ? loadedData.GrandTotalScore
                : _data.GrandTotalScore;

            _data = loadedData;
            _data.GrandTotalScore = highestGrandTotal;

            _isLoaded.Value = true;
        }

        public void SetHasSeenTutorial(bool hasSeenTutorial)
        {
            if (_data.HasSeenTutorial == hasSeenTutorial)
            {
                return;
            }

            _data.HasSeenTutorial = hasSeenTutorial;
            _changed.OnNext(Unit.Default);
        }

        public void SetGrandTotalScore(float grandTotalScore)
        {
            // Monotonic by construction, so nothing can lower the total by writing a stale one.
            if (grandTotalScore <= _data.GrandTotalScore)
            {
                return;
            }

            _data.GrandTotalScore = grandTotalScore;
            _changed.OnNext(Unit.Default);
        }

        public void SetHasCompletedAllLevels(bool hasCompletedAllLevels)
        {
            // One way only. Replaying from the start does not un-finish the game.
            if (!hasCompletedAllLevels || _data.HasCompletedAllLevels)
            {
                return;
            }

            _data.HasCompletedAllLevels = true;
            _changed.OnNext(Unit.Default);
        }

        public void SetCurrentLevelIndex(int currentLevelIndex)
        {
            // Not monotonic: levels roll over, so this can legitimately go backwards.
            if (_data.CurrentLevelIndex == currentLevelIndex)
            {
                return;
            }

            _data.CurrentLevelIndex = currentLevelIndex;
            _changed.OnNext(Unit.Default);
        }
    }
}
