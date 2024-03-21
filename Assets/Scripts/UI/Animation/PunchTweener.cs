using DG.Tweening;
using UnityEngine;
using Utils;
using Zenject;

namespace UI.Animation
{
    [RequireComponent(typeof(RectTransform))]
    public class PunchTweener : TweenerBase<RectTransform>, IInitializable
    {
        [Header("Config")]
        [SerializeField]
        private PunchTweenConfig _config;

        private Vector3 _defaultScale;

        [Impject]
        public void Initialize()
        {
            TargetComponent = GetComponent<RectTransform>();
            _defaultScale = TargetComponent.localScale;
        }

        public void Play()
        {
            KillTween();
            TargetComponent.localScale = _defaultScale;

            Tweener = TargetComponent
                .DOPunchScale(_config.PunchIntensity, _config.DurationSeconds, _config.Vibrato, _config.Elasticity)
                .SetEase(_config.Ease)
                .SetLoops(_config.Loops, _config.LoopType);
        }
    }
}