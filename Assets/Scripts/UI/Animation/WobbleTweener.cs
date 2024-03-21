using DG.Tweening;
using UnityEngine;
using Utils;
using Zenject;

namespace UI.Animation
{
    [RequireComponent(typeof(RectTransform))]
    public class WobbleTweener : TweenerBase<RectTransform>, IInitializable
    {
        [Header("Config")]
        [SerializeField]
        private WobbleTweenConfig _config;

        private Vector3 _defaultEulerAngles;

        [Impject]
        public void Initialize()
        {
            TargetComponent = GetComponent<RectTransform>();
            _defaultEulerAngles = TargetComponent.localEulerAngles;
        }

        public void TogglePlay(bool reset = false)
        {
            KillTween();

            if (reset)
            {
                ResetWobble();
                return;
            }

            var wobbleTweenForward = TargetComponent
                .DOLocalRotate(Vector3.forward * _config.TargetZRotation, _config.DurationSeconds)
                .SetEase(_config.Ease)
                .SetLoops(2, LoopType.Yoyo);
            var wobbleTweenBack = TargetComponent
                .DOLocalRotate(Vector3.back * _config.TargetZRotation, _config.DurationSeconds)
                .SetEase(_config.Ease)
                .SetLoops(2, LoopType.Yoyo);

            Tweener = DOTween.Sequence()
                .Append(wobbleTweenForward)
                .Append(wobbleTweenBack)
                .SetLoops(_config.Loops, _config.LoopType)
                .OnKill(() =>
                {
                    wobbleTweenForward.Kill();
                    wobbleTweenBack.Kill();
                });
        }

        private void ResetWobble()
        {
            TargetComponent.DOLocalRotate(_defaultEulerAngles, _config.DurationSeconds).SetEase(_config.Ease);
        }
    }
}