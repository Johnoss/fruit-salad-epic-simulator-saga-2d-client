using Board;
using DG.Tweening;
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
        
        [Header("Game Over Panel")]
        [SerializeField]
        private TextMeshProUGUI _scoreText;
        [SerializeField]
        private TextMeshProUGUI _highScoreText;
        [SerializeField]
        private GameObject _highScoreBeaten;
        [SerializeField]
        private Button _restartButton;
        [SerializeField]
        private AnchorPositionTweener _goalCompletedTweener;

        [SerializeField]
        private FadeTweener _goalCompletedFadeTweener;

        [Impject]
        private GoalModel _goalModel;
        [Impject]
        private ScoreModel _scoreModel;

        [Impject]
        private ScoreController _scoreController;
        [Impject]
        private GoalController _goalController;
        [Impject]
        private BoardController _boardController;
        
        [Impject]
        private GoalConfig _goalConfig;


        [Impject]
        public void Initialize()
        {
            _goalModel.CollectedTiles.Subscribe(UpdateProgress).AddTo(Disposer);
            _goalModel.GoalReached.Subscribe(ToggleGameOverPanel).AddTo(Disposer);
            
            _scoreModel.Score.Subscribe(UpdateScoreText).AddTo(Disposer);

            _goalModel.GoalReached.DelayFrame(1).WhereTrue().SubscribeBlind(SetHighScore).AddTo(Disposer);
            _restartButton.OnClickAsObservable().SubscribeBlind(FakeRestart).AddTo(Disposer);
        }

        private void SetHighScore()
        {
            var previousHighScore = _scoreModel.GetHighScore();
            _highScoreText.text = $"{previousHighScore:F0}";
            var highScoreBeaten = _scoreModel.Score.Value >= previousHighScore;
            _highScoreBeaten.SetActive(highScoreBeaten);
            if (!highScoreBeaten)
            {
                return;
            }
            
            _highScoreText.text = $"{_scoreModel.Score.Value:F0}";
            _scoreModel.SetHighScore(_scoreModel.Score.Value);
        }

        private void UpdateProgress(int totalCollectedTiles)
        {
            var completedPercentage = Mathf.Clamp((float)totalCollectedTiles / _goalConfig.TilesCollectedGoal, 0f, 1f);

            _progressFillImage.DOFillAmount(completedPercentage, 1f).SetEase(Ease.InSine);

            // _goalText.rectTransform.DORotate(Vector3.back * completedPercentage * 25, .5f).SetEase(Ease.InSine);
            // _goalText.rectTransform.DOScale(Vector3.one + Vector3.one * completedPercentage, .5f)
            //     .SetEase(Ease.InSine);
            
            var remainingTiles = Mathf.Max(_goalConfig.TilesCollectedGoal - totalCollectedTiles, 0);
            
            _goalText.text = $"{remainingTiles} <size=50><color=yellow>left </color></size>";
            
            _goalImagePunchTweener.Play();
        }

        private void FakeRestart()
        {
            _scoreController.RestartScore();
            _goalController.RestartGoal();
            _boardController.RepopulateBoard();
        }

        private void UpdateScoreText(float score)
        {
            _scoreText.text = $"{score:F0}";
        }

        private void ToggleGameOverPanel(bool shouldShow)
        {
            _goalCompletedTweener.Play(!shouldShow);
            _goalCompletedFadeTweener.Play(!shouldShow);
        }
    }
}