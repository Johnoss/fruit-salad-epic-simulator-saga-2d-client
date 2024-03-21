using UnityEngine;

namespace Score
{
    [CreateAssetMenu(menuName = "Create GoalConfig", fileName = "GoalConfig", order = 0)]
    public class GoalConfig : ScriptableObject
    {
        [Header("Balancing")]
        [SerializeField]
        private int _tilesCollectedGoal;
        public int TilesCollectedGoal => _tilesCollectedGoal;
    }
}