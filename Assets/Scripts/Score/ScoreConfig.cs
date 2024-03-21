using UnityEngine;

namespace Score
{
    [CreateAssetMenu(menuName = "Create ScoreConfig", fileName = "ScoreConfig", order = 0)]
    public class ScoreConfig : ScriptableObject
    {

        [Header("Balancing")]
        [SerializeField]
        [Tooltip("Min tiles to be linked to enable collection")]
        private int _minChain = 3;
        [SerializeField]
        [Tooltip("Score for a single tile before chain bonus are applied")]
        private float _collectTileScore = 100;
        [SerializeField]
        [Tooltip("Base value to be used to calculate chain bonus by formula: CollectTileScore + ChainBonusCoefficient * Mathf.Pow(ExtraChain, 2);")]
        private float _chainBonusCoefficient = 50;
        
        public int MinChain => _minChain;
        public float CollectTileScore => _collectTileScore;
        public float ChainBonusCoefficient => _chainBonusCoefficient;
    }
}