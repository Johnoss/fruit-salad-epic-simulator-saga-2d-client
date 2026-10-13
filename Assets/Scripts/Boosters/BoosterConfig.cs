using UnityEngine;

namespace Boosters
{
    [CreateAssetMenu(menuName = "Create BoosterConfig", fileName = "BoosterConfig", order = 0)]
    public class BoosterConfig : ScriptableObject
    {
        [Header("Bigger Bowl")]
        [SerializeField]
        [Tooltip("Extra tiles added to the bowl capacity per purchase")]
        private int _bowlIncrease = 50;

        [Header("Starfruit")]
        [SerializeField]
        [Tooltip("How many Starfruit tiles a single rewarded ad grants")]
        private int _starfruitPerReward = 5;

        [SerializeField]
        [Tooltip("Standard deviation of the placement spread, in tiles, around the board centre. " +
                 "Lower values cluster placements harder into the middle. Roughly a third of the " +
                 "grid width is a good starting point.")]
        private float _starfruitSpread = 1.2f;

        [SerializeField]
        [Tooltip("How many times to resample a placement before giving up and taking the valid " +
                 "node nearest the centre instead")]
        private int _starfruitPlacementAttempts = 24;

        [SerializeField]
        [Tooltip("Each Starfruit lands after its own random delay between zero and this, so a " +
                 "batch scatters in rather than appearing all at once. Its sound plays with it.")]
        private float _starfruitSpawnDelayMax = 0.5f;

        [Header("Rewarded ad IDs")]
        [SerializeField]
        private string _biggerBowlRewardId = "bigger_bowl";

        [SerializeField]
        private string _starfruitRewardId = "starfruit";

        public int BowlIncrease => _bowlIncrease;
        public int StarfruitPerReward => _starfruitPerReward;
        public float StarfruitSpread => _starfruitSpread;
        public int StarfruitPlacementAttempts => _starfruitPlacementAttempts;
        public float StarfruitSpawnDelayMax => _starfruitSpawnDelayMax;
        public string BiggerBowlRewardId => _biggerBowlRewardId;
        public string StarfruitRewardId => _starfruitRewardId;
    }
}
