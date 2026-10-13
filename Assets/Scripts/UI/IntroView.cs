using DG.Tweening;
using MVC;
using Platform;
using Sound;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Zenject;

namespace UI
{
    /// <summary>
    /// Makes the intro screen skippable. The intro normally ends by deactivating its own canvas
    /// from the last tween's onComplete, so skipping is that same end state, reached early.
    /// </summary>
    public class IntroView : AbstractView, IInitializable
    {
        [Header("Intro")]
        [SerializeField]
        [Tooltip("The intro canvas root, deactivated on skip. This is the same object the intro's " +
                 "final tween deactivates when it plays out normally.")]
        private GameObject _introRoot;

        [SerializeField]
        [Tooltip("Full-screen transparent button covering the intro. An Image with alpha 0 still " +
                 "receives raycasts, so it does not need to be visible.")]
        private Button _skipButton;

        [Header("Audio")]
        [SerializeField]
        private SoundView _soundView;

        [SerializeField]
        [Tooltip("Seconds to fade the intro sting out over when skipped")]
        private float _soundFadeSeconds = 0.35f;

        // Optional so the scene still boots without the platform installer wired.
        [InjectOptional]
        private GameLifecycleController _gameLifecycleController;

        private bool _isSkipped;

        [Impject]
        public void Initialize()
        {
            _skipButton.OnClickAsObservable().SubscribeBlind(Skip).AddTo(Disposer);

            // Covers the case where the intro is disabled before this ever runs - otherwise nothing
            // would report and the spinner would never clear.
            if (_introRoot != null && !_introRoot.activeInHierarchy)
            {
                ReportIntroComplete();
            }
        }

        /// <summary>
        /// The intro ends two ways: the tween chain deactivates the canvas from its final
        /// onComplete, or the player skips. Both end in this component's GameObject being disabled,
        /// so hooking that catches both without the two paths having to agree on anything.
        /// </summary>
        private void OnDisable()
        {
            ReportIntroComplete();
        }

        private void ReportIntroComplete()
        {
            _gameLifecycleController?.ReportIntroComplete();
        }

        private void Skip()
        {
            if (_isSkipped)
            {
                return;
            }

            _isSkipped = true;

            // Kill the tween chain before hiding anything. The intro is a sequence of
            // DOTweenAnimations that hand off through onComplete, and one of them re-triggers
            // PlaySplash from an onPlay callback - left running, it would restart the very sound
            // being faded out here, on a canvas the player has already dismissed.
            KillIntroTweens();

            // Owned by SoundView, not by this view: _introRoot is about to be deactivated, and the
            // fade has to keep running after that.
            if (_soundView != null)
            {
                _soundView.FadeOutSplash(_soundFadeSeconds);
            }

            _introRoot.SetActive(false);
        }

        private void KillIntroTweens()
        {
            // Inactive included - later steps in the chain have not been enabled yet.
            foreach (var introAnimation in _introRoot.GetComponentsInChildren<DOTweenAnimation>(true))
            {
                introAnimation.DOKill();
            }
        }
    }
}
