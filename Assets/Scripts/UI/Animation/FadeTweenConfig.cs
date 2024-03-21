using UnityEngine;

namespace UI.Animation
{
    [CreateAssetMenu(menuName = "Tweens/Create FadeTweenConfig", fileName = "FadeTweenConfig", order = 0)]
    public class FadeTweenConfig : AbstractTweenConfig
    {
        [Header("Fade")]
        [SerializeField]
        private float _targetAlpha;
        [SerializeField]
        private float _defaultAlpha;

        public float TargetAlpha => _targetAlpha;
        public float DefaultAlpha => _defaultAlpha;
    }
}