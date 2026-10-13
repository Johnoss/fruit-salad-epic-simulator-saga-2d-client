using Tile;
using UniRx;
using UnityEngine;

namespace FruitGoal
{
    /// <summary>
    /// The live state of one of a level's fruit objectives: which fruit it wants, how many, and how
    /// many have arrived so far.
    ///
    /// The count commits when a collected fruit lands in the bowl, not when it is collected, so the
    /// bar advances in step with the fruit arriving rather than running ahead of the animation.
    /// Surplus arrivals are refused rather than clamped afterwards, which is what lets the flight
    /// stay dumb and fly every collected tile.
    /// </summary>
    public class FruitObjective
    {
        public TileType Fruit { get; }
        public int RequiredCount { get; }

        private readonly IReactiveProperty<int> _collectedCount = new ReactiveProperty<int>();
        public IReadOnlyReactiveProperty<int> CollectedCount => _collectedCount;

        public readonly IReadOnlyReactiveProperty<bool> IsCompleted;

        public FruitObjective(TileType fruit, int requiredCount)
        {
            Fruit = fruit;
            RequiredCount = Mathf.Max(1, requiredCount);

            IsCompleted = _collectedCount
                .Select(collectedCount => collectedCount >= RequiredCount)
                .ToReactiveProperty();
        }

        /// <summary>
        /// Counts one arrival. Returns false when this objective is already full, so the caller can
        /// offer the same arrival to the next objective wanting the same fruit.
        /// </summary>
        public bool TryCount()
        {
            if (_collectedCount.Value >= RequiredCount)
            {
                return false;
            }

            _collectedCount.Value++;

            return true;
        }

        public void Reset()
        {
            _collectedCount.Value = 0;
        }
    }
}
