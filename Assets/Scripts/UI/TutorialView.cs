using Interaction;
using MVC;
using Platform;
using Save;
using UniRx;
using UnityEngine;
using Utils;
using Zenject;

namespace UI
{
    /// <summary>
    /// First-launch tutorial. Two pages, advanced by a tap anywhere, then dismissed and remembered.
    /// </summary>
    public class TutorialView : AbstractView, IInitializable
    {
        [Header("Pages")]
        [SerializeField]
        private GameObject _root;

        [SerializeField]
        private GameObject _page1;

        [SerializeField]
        private GameObject _page2;

        [Impject]
        private SaveModel _saveModel;

        [Impject]
        private InputModel _inputModel;

        [Impject]
        private Score.GoalModel _goalModel;

        // Optional so the scene still boots without the platform installer.
        [InjectOptional]
        private GameLifecycleController _gameLifecycleController;

        private bool _isShowingSecondPage;
        private bool _isDismissed;

        [Impject]
        public void Initialize()
        {
            // Hidden immediately, whatever the scene had it set to. Showing first and hiding once
            // the save arrives would flash the tutorial at every returning player.
            SetVisible(false);

            var introCompleted = _gameLifecycleController != null
                ? _gameLifecycleController.IntroCompleted
                : Observable.Return(Unit.Default);

            // Needs three things: the save, to know whether it has been seen; the intro to be
            // gone, or the tap that skips it would be swallowed as the tutorial's first tap; and
            // the level preview to be dismissed, because explaining the board over a panel that
            // covers it helps nobody.
            _saveModel.IsLoaded.WhereTrue()
                .Zip(introCompleted, (_, __) => Unit.Default)
                .First()
                .SelectMany(_ => _goalModel.IsLevelPreviewVisible.WhereFalse().First())
                .SubscribeBlind(ShowIfUnseen)
                .AddTo(Disposer);
        }

        private void ShowIfUnseen()
        {
            if (_saveModel.Data.HasSeenTutorial)
            {
                return;
            }

            SetVisible(true);

            // The panel is smaller than the screen, so it cannot be relied on to swallow taps that
            // land outside it. Blocking the board explicitly is what makes "tap anywhere" safe.
            _inputModel.SetIsBlocked(InputBlockReason.Tutorial, true);

            // Skips the frame the tutorial appeared on, so the tap that dismissed the intro cannot
            // also advance the first page.
            Observable.EveryUpdate()
                .Skip(1)
                .Where(_ => !_isDismissed && GetIsTapStarted())
                .SubscribeBlind(Advance)
                .AddTo(Disposer);
        }

        private static bool GetIsTapStarted()
        {
            if (Input.GetMouseButtonDown(0))
            {
                return true;
            }

            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        }

        private void Advance()
        {
            if (!_isShowingSecondPage)
            {
                _isShowingSecondPage = true;

                if (_page1 != null) _page1.SetActive(false);
                if (_page2 != null) _page2.SetActive(true);

                return;
            }

            Dismiss();
        }

        private void Dismiss()
        {
            _isDismissed = true;

            SetVisible(false);
            _inputModel.SetIsBlocked(InputBlockReason.Tutorial, false);

            // SaveController persists off SaveModel.Changed, so marking it here is the whole of it -
            // no explicit save call, and nothing to forget.
            _saveModel.SetHasSeenTutorial(true);
        }

        private void SetVisible(bool isVisible)
        {
            if (_root != null)
            {
                _root.SetActive(isVisible);
            }

            if (_page1 != null) _page1.SetActive(isVisible);
            if (_page2 != null) _page2.SetActive(false);
        }
    }
}
