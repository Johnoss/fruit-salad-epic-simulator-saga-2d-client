using JetBrains.Annotations;
using MVC;
using UniRx;
using UnityEngine;

namespace Score
{
    [UsedImplicitly]
    public class ScoreModel : AbstractModel
    {
        private readonly IReactiveProperty<float> _score = new ReactiveProperty<float>();
        public IReadOnlyReactiveProperty<float> Score => _score;

        public void UpdateScore(float deltaScore)
        {
            SetScore(_score.Value + deltaScore);
        }

        public void SetScore(float score)
        {
            _score.Value = score;
        }

        public void SetHighScore(float score)
        {
            PlayerPrefs.SetFloat("HighScore", score);
        }
        
        public float GetHighScore()
        {
            return PlayerPrefs.GetFloat("HighScore", 0f);
        }
    }
}