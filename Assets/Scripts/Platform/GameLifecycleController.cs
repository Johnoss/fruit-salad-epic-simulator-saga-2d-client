using System;
using Interaction;
using JetBrains.Annotations;
using MVC;
using UniRx;
using UnityEngine;
using Utils;

namespace Platform
{
    /// <summary>
    /// Owns the two lifecycle calls certification checks: firstFrameReady and gameReady.
    ///
    /// They are not interchangeable. firstFrameReady says "something is on screen and the player can
    /// see we are loading"; gameReady says "the player can actually play now", and YouTube's spinner
    /// stays up until it fires. Sending gameReady while the intro is still running would clear the
    /// spinner over a screen nobody can interact with, which is exactly what the rule forbids.
    /// </summary>
    [UsedImplicitly]
    public class GameLifecycleController : AbstractController
    {
        /// Long enough that no legitimate intro is still running, short enough to notice in testing.
        private const float MissingGameReadyWarningSeconds = 20f;

        private readonly IPlatformServices _platformServices;
        private readonly InputModel _inputModel;

        private readonly Subject<Unit> _introCompleted = new();

        /// <summary>
        /// Fires when the intro is finished with, however it ended. Replays for late subscribers,
        /// so anything waiting on it cannot miss the event by being constructed a frame too late.
        /// </summary>
        public IObservable<Unit> IntroCompleted => _hasSentGameReady
            ? Observable.Return(Unit.Default)
            : _introCompleted;

        private bool _hasSentFirstFrameReady;
        private bool _hasSentGameReady;
        private bool _isPaused;
        private float _timeScaleBeforePause = 1f;

        public GameLifecycleController(IPlatformServices platformServices, InputModel inputModel)
        {
            _platformServices = platformServices;
            _inputModel = inputModel;

            _platformServices.Paused.SubscribeBlind(Pause).AddTo(Disposer);
            _platformServices.Resumed.SubscribeBlind(Resume).AddTo(Disposer);

            // End of the first frame: by then the intro canvas has actually rendered, so the claim
            // that something is on screen is true rather than merely imminent.
            Observable
                .NextFrame(FrameCountType.EndOfFrame)
                .SubscribeBlind(SendFirstFrameReady)
                .AddTo(Disposer);

            // Deliberately a warning and not a fallback call. Firing gameReady on a timer could
            // clear the spinner while a splash is still up, turning a wiring mistake into a
            // certification failure. Better to shout than to guess.
            Observable
                .Timer(TimeSpan.FromSeconds(MissingGameReadyWarningSeconds))
                .Where(_ => !_hasSentGameReady)
                .SubscribeBlind(WarnGameReadyNeverSent)
                .AddTo(Disposer);
        }

        /// <summary>
        /// Called when the intro is finished with, however it ended - played out or skipped. Safe to
        /// call more than once.
        /// </summary>
        public void ReportIntroComplete()
        {
            SendGameReady();
        }

        private void SendFirstFrameReady()
        {
            if (_hasSentFirstFrameReady)
            {
                return;
            }

            _hasSentFirstFrameReady = true;
            _platformServices.NotifyFirstFrameReady();
        }

        private void SendGameReady()
        {
            if (_hasSentGameReady)
            {
                return;
            }

            // Ordering matters if the intro is skipped on the very first frame: the platform
            // expects to hear about the first frame before it hears the game is ready.
            SendFirstFrameReady();

            _hasSentGameReady = true;
            _platformServices.NotifyGameReady();

            _introCompleted.OnNext(Unit.Default);
        }

        /// <summary>
        /// timeScale rather than a bespoke pause flag, because the requirement is that timers stop
        /// too. DOTween tweens and UniRx timers on the default scheduler all follow it, so the
        /// board, the flying sprites and the Starfruit spawn stagger freeze together. The intro
        /// fade and the stubbed ad timer deliberately run unscaled and keep going.
        /// </summary>
        private void Pause()
        {
            if (_isPaused)
            {
                return;
            }

            _isPaused = true;

            // Captured rather than assumed: restoring a hardcoded 1 would silently undo any other
            // time scaling that happened to be in effect.
            _timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;

            // Freezes audio in place rather than silencing it, which is what a pause should do -
            // and is separate from the mute toggle, which works on listener volume.
            AudioListener.pause = true;

            _inputModel.SetIsBlocked(InputBlockReason.PlatformPause, true);
        }

        private void Resume()
        {
            if (!_isPaused)
            {
                return;
            }

            _isPaused = false;

            Time.timeScale = _timeScaleBeforePause;
            AudioListener.pause = false;

            _inputModel.SetIsBlocked(InputBlockReason.PlatformPause, false);
        }

        private void WarnGameReadyNeverSent()
        {
            Debug.LogWarning(
                $"[GameLifecycle] gameReady still unsent after {MissingGameReadyWarningSeconds}s. " +
                "YouTube's loading spinner stays up until it fires, so the game reads as broken. " +
                "Check that an IntroView is present and reporting when the intro ends.");
        }
    }
}
