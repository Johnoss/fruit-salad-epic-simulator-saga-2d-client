using JetBrains.Annotations;
using MVC;
using UniRx;
using UnityEngine;

namespace Score
{
    [UsedImplicitly]
    public class GoalModel : AbstractModel
    {
        private readonly IReactiveProperty<int> _collectedTiles = new ReactiveProperty<int>();

        /// <summary>
        /// Goal fruit in the bowl, summed across the level's objectives and capped per objective.
        /// Commits on arrival rather than on collection, because it is the objectives added up and
        /// they commit on arrival - so the bowl fills in step with the fruit landing in it.
        /// </summary>
        public IReadOnlyReactiveProperty<int> CollectedTiles => _collectedTiles;

        private readonly IReactiveProperty<int> _tilesGoal = new ReactiveProperty<int>(1);

        /// <summary>
        /// What a full bowl is worth: everything the level's objectives ask for, added up. Comes
        /// from the level rather than from config now, so a level with lighter objectives fills a
        /// bowl just as completely as a heavy one.
        /// </summary>
        public IReadOnlyReactiveProperty<int> TilesGoal => _tilesGoal;

        private readonly IReactiveProperty<bool> _isLevelComplete = new ReactiveProperty<bool>();

        /// <summary>
        /// Whether every fruit objective is done. This is what ends the level - filling the bowl no
        /// longer does, because the bowl now measures the objectives rather than capping the run.
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsLevelComplete => _isLevelComplete;

        private readonly IReactiveProperty<bool> _isLevelPreviewVisible = new ReactiveProperty<bool>();

        /// <summary>
        /// Whether the level preview panel is on screen. It shows on launch over the first level
        /// and again after every completion, so it is not derived from IsLevelComplete - and even
        /// after a completion it is deliberately late, the level ending first.
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsLevelPreviewVisible => _isLevelPreviewVisible;

        /// <summary>
        /// Sets both halves of the bowl at once. They move together - starting a level replaces the
        /// objectives and so changes what a full bowl is worth - and setting them separately would
        /// draw a frame at the new count against the old total.
        /// </summary>
        public void SetBowlProgress(int collectedTiles, int tilesGoal)
        {
            // Clamped away from zero because everything reading this divides by it.
            _tilesGoal.Value = Mathf.Max(1, tilesGoal);
            _collectedTiles.Value = Mathf.Max(0, collectedTiles);
        }

        public void SetIsLevelComplete(bool isLevelComplete)
        {
            _isLevelComplete.Value = isLevelComplete;
        }

        public void SetIsLevelPreviewVisible(bool isLevelPreviewVisible)
        {
            _isLevelPreviewVisible.Value = isLevelPreviewVisible;
        }
    }
}
