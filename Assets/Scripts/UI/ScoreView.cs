using DG.Tweening;
using MVC;
using Score;
using TMPro;
using UniRx;
using UnityEngine;
using Utils;
using Zenject;

namespace UI
{
    public class ScoreView : AbstractView, IInitializable
    {
        [Header("Text")]
        [SerializeField]
        private TextMeshProUGUI _scoreText;

        [Impject]
        private ScoreModel _scoreModel;
        
        [Impject]
        public void Initialize()
        {
            _scoreModel.Score.Subscribe(UpdateScoreText).AddTo(Disposer);
        }

        private void UpdateScoreText(float score)
        {
            _scoreText.DOText($"{score:F0}", .2f, true, ScrambleMode.Numerals);
        }
    }
}