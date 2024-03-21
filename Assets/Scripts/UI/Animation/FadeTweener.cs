using DG.Tweening;
using UnityEngine;
using Utils;
using Zenject;

namespace UI.Animation
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeTweener : TweenerBase<CanvasGroup>, IInitializable
    {
        [Header("Config")]
        [SerializeField]
        private FadeTweenConfig _config;

        [Impject]
        public void Initialize()
        {
            TargetComponent = GetComponent<CanvasGroup>();
        }

        public void Play(bool reverse = false)
        {
            KillTween();

            TargetComponent.alpha = reverse ? _config.TargetAlpha : _config.DefaultAlpha;
            var targetAlpha = reverse ? _config.DefaultAlpha : _config.TargetAlpha;

            Tweener = TargetComponent.DOFade(targetAlpha, _config.DurationSeconds)
                .SetEase(_config.Ease)
                .SetLoops(_config.Loops, _config.LoopType);
        }
    }
}