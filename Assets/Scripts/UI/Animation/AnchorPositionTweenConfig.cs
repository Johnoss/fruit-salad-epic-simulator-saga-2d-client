using UnityEngine;

namespace UI.Animation
{
    [CreateAssetMenu(menuName = "Tweens/Create AnchorPositionTweenConfig", fileName = "AnchorPositionTweenConfig", order = 0)]
    public class AnchorPositionTweenConfig : AbstractTweenConfig
    {
        [Header("Position")]
        [SerializeField]
        private Vector2 _targetVectorPosition = Vector2.zero;

        public Vector2 TargetVectorPosition => _targetVectorPosition;
    }
}