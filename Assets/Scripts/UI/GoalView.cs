using DG.Tweening;
using Level;
using MVC;
using Score;
using TMPro;
using UI.Animation;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Zenject;

namespace UI
{
    public class GoalView : AbstractView, IInitializable
    {
        [Header("Text")]
        [SerializeField]
        private TextMeshProUGUI _goalText;
        
        [Header("Visuals")]
        [SerializeField]
        private Image _progressFillImage;
        [SerializeField]
        private PunchTweener _goalImagePunchTweener;

        [Header("Level Preview Panel")]
        [SerializeField]
        private TextMeshProUGUI _scoreText;
        [SerializeField]
        private AnchorPositionTweener _goalCompletedTweener;

        [SerializeField]
        private FadeTweener _goalCompletedFadeTweener;

        [Header("End of content")]
        [SerializeField]
        [Tooltip("Shown from the moment the last level has been finished, and from then on. This " +
                 "is the finite-content signal certification asks for, so it has to be plain: " +
                 "something like \"You have finished all 100 levels\".")]
        private GameObject _contentCompleteMessage;

        [Impject]
        private GoalModel _goalModel;
        [Impject]
        private ScoreModel _scoreModel;
        [Impject]
        private LevelModel _levelModel;


        [Impject]
        public void Initialize()
        {
            _goalModel.CollectedTiles.SubscribeBlind(UpdateProgress).AddTo(Disposer);
            // Starting a level replaces the objectives and so changes what a full bowl is worth,
            // which means the bar has to redraw on goal changes too, not just on arrivals.
            _goalModel.TilesGoal.SubscribeBlind(UpdateProgress).AddTo(Disposer);

            // Punching is feedback for a goal fruit landing, which is the only thing that moves
            // the count now.
            _goalModel.CollectedTiles.SubscribeBlind(PlayGoalPunch).AddTo(Disposer);
            // Panel follows IsLevelPreviewVisible, not IsLevelComplete: it is also the launch
            // screen, and after a completion it arrives a beat late with input blocked in between.
            _goalModel.IsLevelPreviewVisible.Subscribe(ToggleLevelPreviewPanel).AddTo(Disposer);
            
            _scoreModel.Score.Subscribe(UpdateScoreText).AddTo(Disposer);
            _levelModel.IsContentComplete.Subscribe(ToggleContentCompleteMessage).AddTo(Disposer);
        }

        private void UpdateProgress()
        {
            var totalCollectedTiles = _goalModel.CollectedTiles.Value;
            var tilesGoal = Mathf.Max(1, _goalModel.TilesGoal.Value);

            var completedPercentage = Mathf.Clamp((float)totalCollectedTiles / tilesGoal, 0f, 1f);

            if (_progressFillImage != null)
            {
                _progressFillImage.DOFillAmount(completedPercentage, 1f).SetEase(Ease.InSine);
            }

            // _goalText.rectTransform.DORotate(Vector3.back * completedPercentage * 25, .5f).SetEase(Ease.InSine);
            // _goalText.rectTransform.DOScale(Vector3.one + Vector3.one * completedPercentage, .5f)
            //     .SetEase(Ease.InSine);

            // Optional now that the objective bars carry the readable progress. The label is the
            // old "N left" on the bowl, which the bars replace, so a scene that has removed it is
            // a valid setup rather than a missing reference.
            if (_goalText == null)
            {
                return;
            }

            var remainingTiles = Mathf.Max(tilesGoal - totalCollectedTiles, 0);

            _goalText.text = $"{remainingTiles} <size=50><color=yellow>left </color></size>";
        }

        private void PlayGoalPunch()
        {
            if (_goalImagePunchTweener == null)
            {
                return;
            }

            _goalImagePunchTweener.Play();
        }

        private void UpdateScoreText(float score)
        {
            if (_scoreText == null)
            {
                return;
            }

            _scoreText.text = $"{score:F0}";
        }

        private void ToggleContentCompleteMessage(bool isContentComplete)
        {
            if (_contentCompleteMessage == null)
            {
                return;
            }

            _contentCompleteMessage.SetActive(isContentComplete);
        }

        private void ToggleLevelPreviewPanel(bool shouldShow)
        {
            if (_goalCompletedTweener != null)
            {
                _goalCompletedTweener.Play(!shouldShow);
            }

            if (_goalCompletedFadeTweener != null)
            {
                _goalCompletedFadeTweener.Play(!shouldShow);
            }
        }
    }
}