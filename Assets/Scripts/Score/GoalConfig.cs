using UnityEngine;

namespace Score
{
    [CreateAssetMenu(menuName = "Create GoalConfig", fileName = "GoalConfig", order = 0)]
    public class GoalConfig : ScriptableObject
    {
        [Header("Balancing")]
        [SerializeField]
        [Tooltip("Pause between the level ending and the game over panel appearing. Input is blocked " +
                 "for the whole pause, so the last collection can land without the panel snapping in " +
                 "on the same frame the player lifts their finger.")]
        private float _gameOverDelaySeconds = 1.2f;

        public float GameOverDelaySeconds => _gameOverDelaySeconds;
    }
}
