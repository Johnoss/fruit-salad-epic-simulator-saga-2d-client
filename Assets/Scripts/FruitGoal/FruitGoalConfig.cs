using UnityEngine;
using UnityEngine.Serialization;

namespace FruitGoal
{
    [CreateAssetMenu(menuName = "Create FruitGoalConfig", fileName = "FruitGoalConfig", order = 0)]
    public class FruitGoalConfig : ScriptableObject
    {
        [Header("Balancing")]
        [SerializeField]
        [FormerlySerializedAs("_requiredCount")]
        [Tooltip("Fallback for an objective that does not state its own count. Levels normally set " +
                 "the count per objective; this is what they get if they leave it at zero.")]
        private int _defaultRequiredCount = 15;

        public int DefaultRequiredCount => _defaultRequiredCount;
    }
}
