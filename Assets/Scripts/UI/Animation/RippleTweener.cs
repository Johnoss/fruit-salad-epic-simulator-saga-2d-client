using DG.Tweening;
using UnityEngine;
using Utils;
using Zenject;

namespace UI.Animation
{
    [RequireComponent(typeof(RectTransform))]
    public class RippleTweener : TweenerBase<RectTransform>, IInitializable
    {
        [Header("Config")]
        [SerializeField]
        private RippleTweenConfig _config;

        private Vector3 _defaultScale;

        [Impject]
        public void Initialize()
        {
            TargetComponent = GetComponent<RectTransform>();
            _defaultScale = TargetComponent.localScale;
        }

        public void Play(float distance)
        {
            if (distance <= 0)
            {
                return;
            }

            KillTween();

            var intensity =
                Vector3.one * (_config.PunchIntensity / (distance * _config.IntensityLossByDistance));
            
            var delaySeconds = _config.DistanceDelaySecondsMultiplier * distance * distance;
            
            Tweener = TargetComponent
                .DOPunchScale(intensity, _config.DurationSeconds, 1)
                .SetDelay(delaySeconds)
                .SetEase(_config.Ease)
                .SetLoops(_config.Loops, _config.LoopType);
        }

        public override void KillTween()
        {
            TargetComponent.localScale = _defaultScale;
            base.KillTween();
        }
    }
}