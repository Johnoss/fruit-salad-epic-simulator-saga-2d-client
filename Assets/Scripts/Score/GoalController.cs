using System;
using Interaction;
using JetBrains.Annotations;
using MVC;
using UniRx;
using UnityEngine;
using Utils;

namespace Score
{
    [UsedImplicitly]
    public class GoalController : AbstractController
    {
        private readonly GoalModel _model;
        private readonly GoalConfig _goalConfig;
        private readonly InputModel _inputModel;

        private IDisposable _pendingPreview;

        public GoalController(GoalModel model, GoalConfig goalConfig, InputModel inputModel)
        {
            _model = model;
            _goalConfig = goalConfig;
            _inputModel = inputModel;

            // Running out of content needs no path of its own: the last level ending is a level
            // ending, so it already comes through here and the panel says the rest.
            _model.IsLevelComplete
                .DistinctUntilChanged()
                .WhereTrue()
                .SubscribeBlind(EndLevel)
                .AddTo(Disposer);
        }

        /// <summary>
        /// Pushed by the fruit objectives whenever one of them moves. The bowl is their sum rather
        /// than a tally of its own, so nothing here counts collections.
        /// </summary>
        public void SetBowlProgress(int collectedTiles, int tilesGoal)
        {
            _model.SetBowlProgress(collectedTiles, tilesGoal);
        }

        /// <summary>
        /// Raised by the fruit objectives when the last of them completes, and lowered again when
        /// they are reset. Held here rather than read from the objectives directly so the panel,
        /// the input block and the end-of-content signal all keep meeting at one place.
        /// </summary>
        public void SetIsLevelComplete(bool isLevelComplete)
        {
            _model.SetIsLevelComplete(isLevelComplete);
        }

        /// <summary>
        /// Puts the preview up now, with no pause first. This is the launch path: there is no last
        /// collection to let play out, so the delay would only be a blank screen.
        /// </summary>
        public void ShowLevelPreview()
        {
            CancelPendingPreview();

            _inputModel.SetIsBlocked(InputBlockReason.GameOver, true);
            _model.SetIsLevelPreviewVisible(true);
        }

        /// Takes the preview down and hands the board back. The level behind it is already built.
        public void HideLevelPreview()
        {
            CancelPendingPreview();

            _model.SetIsLevelPreviewVisible(false);
            _inputModel.SetIsBlocked(InputBlockReason.GameOver, false);
        }

        /// <summary>
        /// Edge triggered, not a bound state. The preview outlives the completion that raised it -
        /// starting the next level lowers IsLevelComplete again, and the panel has to stay up until
        /// the player dismisses it.
        /// </summary>
        private void EndLevel()
        {
            CancelPendingPreview();

            // Input is cut the instant the level ends, not when the panel appears. The delay exists
            // so the last collection can play out, and without the block the player could keep
            // matching into a level that is already over.
            _inputModel.SetIsBlocked(InputBlockReason.GameOver, true);

            var delaySeconds = Mathf.Max(0f, _goalConfig.GameOverDelaySeconds);

            if (delaySeconds <= 0f)
            {
                _model.SetIsLevelPreviewVisible(true);
                return;
            }

            _pendingPreview = Observable
                .Timer(TimeSpan.FromSeconds(delaySeconds))
                .SubscribeBlind(() => _model.SetIsLevelPreviewVisible(true))
                .AddTo(Disposer);
        }

        private void CancelPendingPreview()
        {
            _pendingPreview?.Dispose();
            _pendingPreview = null;
        }
    }
}
