using Interaction;
using JetBrains.Annotations;
using MVC;
using Platform;
using UniRx;

namespace Boosters
{
    [UsedImplicitly]
    public class BoosterController : AbstractController
    {
        private readonly BoosterModel _model;
        private readonly BoosterConfig _config;
        private readonly IPlatformServices _platformServices;
        private readonly StarfruitPlacer _starfruitPlacer;
        private readonly InputModel _inputModel;

        public BoosterController(BoosterModel model, BoosterConfig config, IPlatformServices platformServices,
            StarfruitPlacer starfruitPlacer, InputModel inputModel)
        {
            _model = model;
            _config = config;
            _platformServices = platformServices;
            _starfruitPlacer = starfruitPlacer;
            _inputModel = inputModel;
        }

        /// <summary>
        /// Whether this booster is currently worth offering. The view drives button state from
        /// this rather than repeating the rules, so the two cannot disagree.
        /// </summary>
        public bool GetCanRequest(BoosterType boosterType)
        {
            if (_model.PendingBooster.Value != BoosterType.None)
            {
                return false;
            }

            return boosterType switch
            {
                // Retired along with the bowl's capacity - see GrantBooster.
                BoosterType.BiggerBowl => false,
                BoosterType.Starfruit => true,
                _ => false
            };
        }

        public void RequestBooster(BoosterType boosterType)
        {
            if (!GetCanRequest(boosterType))
            {
                return;
            }

            _model.SetPendingBooster(boosterType);
            _inputModel.SetIsBlocked(InputBlockReason.Ad, true);

            _platformServices.ShowRewardedAd(GetRewardId(boosterType))
                .Take(1)
                .Subscribe(
                    result => ResolveBooster(boosterType, result),
                    _ => ResolveBooster(boosterType, AdResult.Error))
                .AddTo(Disposer);
        }

        private void ResolveBooster(BoosterType boosterType, AdResult result)
        {
            // Cleared before granting, so a booster that somehow throws still releases the UI.
            _model.SetPendingBooster(BoosterType.None);
            _inputModel.SetIsBlocked(InputBlockReason.Ad, false);

            // No fill is normal, not an error. Nothing was consumed and the board is untouched,
            // so the run continues exactly as it was and the player can try again.
            if (result != AdResult.Granted)
            {
                _model.ReportDenied(boosterType);
                return;
            }

            if (!GrantBooster(boosterType))
            {
                _model.ReportDenied(boosterType);
                return;
            }

            _model.ReportGranted(boosterType);
        }

        private bool GrantBooster(BoosterType boosterType)
        {
            switch (boosterType)
            {
                case BoosterType.BiggerBowl:
                    // Retired. The bowl measures the level's objectives now, so there is no
                    // capacity left to buy - enlarging it would only make the level look less
                    // finished than it is. Denied rather than silently granted, and the button
                    // itself still wants removing from the scene.
                    return false;

                case BoosterType.Starfruit:
                    // Placement is staggered, so this reports that the batch started, not that it
                    // finished. Only a board with nowhere to put a single Starfruit counts as a
                    // denial - a batch that lands short still gave the player value.
                    return _starfruitPlacer.TryPlaceStarfruits(_config.StarfruitPerReward);

                default:
                    return false;
            }
        }

        private string GetRewardId(BoosterType boosterType)
        {
            return boosterType switch
            {
                BoosterType.BiggerBowl => _config.BiggerBowlRewardId,
                BoosterType.Starfruit => _config.StarfruitRewardId,
                _ => string.Empty
            };
        }
    }
}
