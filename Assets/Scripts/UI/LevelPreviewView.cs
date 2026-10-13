using System.Collections.Generic;
using FruitGoal;
using Level;
using MVC;
using Score;
using Tile;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Zenject;

namespace UI
{
    /// <summary>
    /// The level preview panel: which level is next, what it asks for, and what the player has
    /// banked so far.
    ///
    /// Shows the level the player is about to play, not the one they just finished - on launch that
    /// is the first level, and after a completion it is the next one. Everything here reads off
    /// LevelModel's preview, which is the only thing that knows the difference.
    /// </summary>
    public class LevelPreviewView : AbstractView, IInitializable
    {
        [Header("Level")]
        [SerializeField]
        [Tooltip("Arg 0 is the level number, counting from one.")]
        private TextMeshProUGUI _levelText;

        [SerializeField]
        private string _levelFormat = "Level {0}";

        [Header("Score")]
        [SerializeField]
        [Tooltip("Every level's score added together. Arg 0 is the total.")]
        private TextMeshProUGUI _grandTotalText;

        [SerializeField]
        private string _grandTotalFormat = "{0}";

        [Header("Play")]
        [SerializeField]
        [Tooltip("Starts whatever the preview is advertising - the first level on launch, the next " +
                 "one after a completion - and the panel animates out behind it.")]
        private Button _playButton;

        [Header("Objectives")]
        [SerializeField]
        [Tooltip("One per objective a level may have, top to bottom. A level with fewer disables " +
                 "the extras.")]
        private List<FruitObjectivePreviewView> _objectivePreviews = new();

        [Impject]
        private LevelModel _levelModel;

        [Impject]
        private ScoreModel _scoreModel;

        [Impject]
        private TileConfig _tileConfig;

        [Impject]
        private FruitGoalConfig _fruitGoalConfig;

        [Impject]
        private LevelController _levelController;

        [Impject]
        public void Initialize()
        {
            _levelModel.PreviewLevelIndex.Subscribe(UpdateLevelText).AddTo(Disposer);
            _levelModel.PreviewLevel.Subscribe(UpdateObjectivePreviews).AddTo(Disposer);

            _scoreModel.GrandTotal.Subscribe(UpdateGrandTotalText).AddTo(Disposer);

            SubscribePlayButton();
        }

        /// <summary>
        /// Hooked up last and guarded, like everything else here. The panel has no other way out,
        /// so an unassigned button is a game that cannot be started - worth an error rather than a
        /// quiet nothing.
        /// </summary>
        private void SubscribePlayButton()
        {
            if (_playButton == null)
            {
                Debug.LogError("[LevelPreviewView] No play button assigned. Nothing can dismiss the " +
                               "preview, so the level cannot be started.", this);
                return;
            }

            // The button only asks for the level. Sliding the panel out is GoalView reacting to
            // IsLevelPreviewVisible, the same path a completion takes to bring it back.
            _playButton.OnClickAsObservable()
                .SubscribeBlind(_levelController.StartPendingLevel)
                .AddTo(Disposer);
        }

        private void UpdateLevelText(int previewLevelIndex)
        {
            if (_levelText == null)
            {
                return;
            }

            // Counting from one, because nobody outside the code calls the first level zero.
            _levelText.text = string.Format(_levelFormat, previewLevelIndex + 1);
        }

        private void UpdateGrandTotalText(float grandTotal)
        {
            if (_grandTotalText == null)
            {
                return;
            }

            _grandTotalText.text = string.Format(_grandTotalFormat, Mathf.RoundToInt(grandTotal));
        }

        private void UpdateObjectivePreviews(LevelDefinition previewLevel)
        {
            var objectives = previewLevel?.Objectives;
            var objectiveCount = objectives?.Count ?? 0;

            if (objectiveCount > _objectivePreviews.Count)
            {
                Debug.LogError($"[LevelPreviewView] {objectiveCount} objectives but only " +
                               $"{_objectivePreviews.Count} preview rows. The level will still ask " +
                               "for them, but the player is not being told.", this);
            }

            for (var index = 0; index < _objectivePreviews.Count; index++)
            {
                var preview = _objectivePreviews[index];

                if (preview == null)
                {
                    Debug.LogError($"[LevelPreviewView] Preview row {index} is unassigned.", this);
                    continue;
                }

                if (index >= objectiveCount)
                {
                    preview.Hide();
                    continue;
                }

                ShowObjective(preview, objectives[index]);
            }
        }

        private void ShowObjective(FruitObjectivePreviewView preview, FruitObjectiveDefinition objective)
        {
            if (objective == null)
            {
                preview.Hide();
                return;
            }

            // Zero means the level left the count alone, and the bars will fall back to the config
            // default - so the preview has to show the same fallback or the two disagree.
            var requiredCount = objective.RequiredCount > 0
                ? objective.RequiredCount
                : _fruitGoalConfig.DefaultRequiredCount;

            preview.Show(_tileConfig.GetTileSpriteOrDefault(objective.Fruit), requiredCount);
        }
    }
}
