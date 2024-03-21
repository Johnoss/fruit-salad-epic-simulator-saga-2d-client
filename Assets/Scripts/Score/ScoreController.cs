using JetBrains.Annotations;
using MVC;
using UnityEngine;

namespace Score
{
    [UsedImplicitly]
    public class ScoreController : AbstractController
    {
        private readonly ScoreModel _model;
        private readonly ScoreConfig _scoreConfig;

        public ScoreController(ScoreModel model, ScoreConfig scoreConfig)
        {
            _model = model;
            _scoreConfig = scoreConfig;
        }

        public void AddCollectedChainScore(int chainLenght)
        {
            _model.UpdateScore(GetScoreForChain(chainLenght));
        }

        public float GetScoreForChain(int chainLenght)
        {
            var totalCollectionScore = 0f;
            for (var i = 1; i <= chainLenght; i++)
            {
                totalCollectionScore += GetScoreByChainIndex(i);
            }
            return totalCollectionScore;
        }
        
        private float GetScoreByChainIndex(int index)
        {
            //when MinChain is 3, first 3 tiles don't have any chain bonus (and only get base amount)
            var baseChainBonus = Mathf.Max(0, index - _scoreConfig.MinChain);
            //take base score and add bonus for long chains (growing exponentially, except first MinChains)
            var totalScore = _scoreConfig.CollectTileScore +
                             _scoreConfig.ChainBonusCoefficient * Mathf.Pow(baseChainBonus, 2);

            return totalScore;
        }

        public void RestartScore()
        {
            _model.SetScore(0);
        }
    }
}