using UnityEngine;

namespace UI.Animation
{
    [CreateAssetMenu(menuName = "Tweens/Create RippleTweenConfig", fileName = "RippleTweenConfig", order = 0)]
    public class RippleTweenConfig : AbstractTweenConfig
    {
        [Header("Base Ripple")]
        [SerializeField]
        private float _punchIntensity;

        
        public float PunchIntensity => _punchIntensity;
        
        [Header("Distance Modifiers")]
        [SerializeField]
        private float _distanceDelaySecondsMultiplier;
        [SerializeField]
        private float _intensityLossByDistance;
        
        public float DistanceDelaySecondsMultiplier => _distanceDelaySecondsMultiplier;
        public float IntensityLossByDistance => _intensityLossByDistance;

    }
}