using DG.Tweening;
using UnityEngine;
using Utils;
using Zenject;

namespace UI.Animation
{
    [RequireComponent(typeof(RectTransform))]
    public class AnchorPositionTweener : TweenerBase<RectTransform>, IInitializable
    {
        [Header("Config")]
        [SerializeField]
        private AnchorPositionTweenConfig _config;

        private Vector2 _defaultAnchorPosition;

        [Impject]
        public void Initialize()
        {
            TargetComponent = GetComponent<RectTransform>();
            _defaultAnchorPosition = TargetComponent.anchoredPosition;
        }

        public void Play(bool reverse = false)
        {
            var targetPosition = reverse ? _defaultAnchorPosition : _config.TargetVectorPosition;
            Play(targetPosition);
        }

        public void Play(Vector2 targetPosition)
        {
            KillTween();
            
            Tweener = TargetComponent
                .DOAnchorPos(targetPosition, _config.DurationSeconds)
                .SetLoops(_config.Loops, _config.LoopType)
                .SetSpeedBased(_config.IsSpeedBased)
                .SetEase(_config.Ease);
        }
    }
}