using JetBrains.Annotations;
using MVC;
using UniRx;

namespace Score
{
    [UsedImplicitly]
    public class GoalModel : AbstractModel
    {
        private readonly IReactiveProperty<int> _collectedTiles = new ReactiveProperty<int>();
        public IReadOnlyReactiveProperty<int> CollectedTiles => _collectedTiles;

        public readonly IReadOnlyReactiveProperty<bool> GoalReached;

        public GoalModel(GoalConfig goalConfig)
        {
            GoalReached = _collectedTiles
                .Select(collectedTiles => collectedTiles >= goalConfig.TilesCollectedGoal)
                .ToReactiveProperty();
        }
        public void UpdateCollected(int deltaCollected)
        {
            SetCollected(_collectedTiles.Value + deltaCollected);
        }

        public void SetCollected(int collected)
        {
            _collectedTiles.Value = collected;
        }
    }
}