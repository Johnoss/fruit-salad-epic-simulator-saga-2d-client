using UnityEngine;

namespace UI.Animation
{
    [CreateAssetMenu(menuName = "Tweens/Create WobbleTweenConfig", fileName = "WobbleTweenConfig", order = 0)]
    public class WobbleTweenConfig : AbstractTweenConfig
    {
        [Header("Rotation")]
        [SerializeField]
        private float _targetZRotation;

        public float TargetZRotation => _targetZRotation;
    }
}