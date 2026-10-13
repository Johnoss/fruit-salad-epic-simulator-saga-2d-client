using JetBrains.Annotations;
using MVC;
using UniRx;

namespace Boosters
{
    [UsedImplicitly]
    public class BoosterModel : AbstractModel
    {
        private readonly IReactiveProperty<BoosterType> _pendingBooster =
            new ReactiveProperty<BoosterType>(BoosterType.None);

        /// The booster whose ad is currently on screen, or None.
        public IReadOnlyReactiveProperty<BoosterType> PendingBooster => _pendingBooster;

        public readonly IReadOnlyReactiveProperty<bool> IsAdInProgress;

        public readonly ISubject<BoosterType> OnBoosterGranted = new Subject<BoosterType>();

        /// Fires when a booster was requested but not granted - no fill, dismissed, or error.
        /// Nothing is consumed, so the player is free to try again.
        public readonly ISubject<BoosterType> OnBoosterDenied = new Subject<BoosterType>();

        public BoosterModel()
        {
            IsAdInProgress = _pendingBooster
                .Select(pendingBooster => pendingBooster != BoosterType.None)
                .ToReactiveProperty();
        }

        public void SetPendingBooster(BoosterType boosterType)
        {
            _pendingBooster.Value = boosterType;
        }

        public void ReportGranted(BoosterType boosterType)
        {
            OnBoosterGranted.OnNext(boosterType);
        }

        public void ReportDenied(BoosterType boosterType)
        {
            OnBoosterDenied.OnNext(boosterType);
        }
    }
}
