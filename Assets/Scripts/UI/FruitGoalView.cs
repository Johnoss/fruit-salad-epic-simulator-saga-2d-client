using System.Collections.Generic;
using DG.Tweening;
using FruitGoal;
using TMPro;
using UI.Animation;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// One objective bar: the fruit it wants, how far along it is, and a tick when it is done.
    ///
    /// Deliberately not injected. There are as many of these as the level has objectives and they
    /// differ only in which objective they are pointed at, so <see cref="FruitGoalPanelView"/> owns
    /// them and binds each one. Everything here reads off the objective it was handed.
    /// </summary>
    public class FruitGoalView : MonoBehaviour
    {
        [Header("Bar")]
        [SerializeField]
        private Slider _progressSlider;

        [SerializeField]
        [Tooltip("Icon showing which fruit this objective wants")]
        private Image _fruitIconImage;

        [SerializeField]
        [Tooltip("Collected over required, drawn inside the bar. Arg 0 is collected, arg 1 required.")]
        private TextMeshProUGUI _progressText;

        [SerializeField]
        private string _progressFormat = "{0}/{1}";

        [SerializeField]
        private float _sliderTweenSeconds = 0.2f;

        [Header("Completion")]
        [SerializeField]
        [Tooltip("Green tick, shown once the objective is full. Toggled on and off, so it can sit " +
                 "wherever it looks right in the bar.")]
        private GameObject _completedTickmark;

        [SerializeField]
        [Tooltip("Optional. Punched when the objective completes.")]
        private PunchTweener _completionPunchTweener;

        private CompositeDisposable _binding;

        /// <summary>
        /// Points this bar at an objective and shows it. Safe to call on an already bound bar - the
        /// previous binding is dropped first - so rebinding on level start needs no teardown pass.
        /// </summary>
        public void Bind(FruitObjective objective, Sprite fruitSprite)
        {
            Unbind();

            if (objective == null)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);

            LogMissingReferences();

            if (_fruitIconImage != null && fruitSprite != null)
            {
                _fruitIconImage.sprite = fruitSprite;
            }

            _binding = new CompositeDisposable();

            // Drawn once directly and only then subscribed, so binding snaps to the objective's
            // current state instead of tweening up from whatever the last objective left behind.
            UpdateProgress(objective, false);
            SetCompleted(objective.IsCompleted.Value, false);

            objective.CollectedCount
                .Skip(1)
                .Subscribe(_ => UpdateProgress(objective, true))
                .AddTo(_binding);

            objective.IsCompleted
                .Skip(1)
                .Subscribe(isCompleted => SetCompleted(isCompleted, true))
                .AddTo(_binding);
        }

        /// <summary>
        /// Switches the bar off for a level with fewer objectives than there are bars. Disabling the
        /// object rather than emptying it is what lets the layout group close the gap.
        /// </summary>
        public void Hide()
        {
            Unbind();

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            _binding?.Dispose();
            _binding = null;

            // Unity's null check, not `?.` - a destroyed slider is not plain-C# null and the
            // operator would sail straight past it.
            if (_progressSlider != null)
            {
                _progressSlider.DOKill();
            }
        }

        private void UpdateProgress(FruitObjective objective, bool isAnimated)
        {
            var collectedCount = Mathf.Min(objective.CollectedCount.Value, objective.RequiredCount);

            if (_progressText != null)
            {
                _progressText.text = string.Format(_progressFormat, collectedCount, objective.RequiredCount);
            }

            if (_progressSlider == null)
            {
                return;
            }

            var progress = Mathf.Clamp01((float)collectedCount / objective.RequiredCount);

            _progressSlider.DOKill();

            if (!isAnimated)
            {
                _progressSlider.value = progress;
                return;
            }

            _progressSlider.DOValue(progress, _sliderTweenSeconds).SetEase(Ease.OutSine);
        }

        private void SetCompleted(bool isCompleted, bool isAnimated)
        {
            if (_completedTickmark != null)
            {
                _completedTickmark.SetActive(isCompleted);
            }

            if (!isCompleted || !isAnimated || _completionPunchTweener == null)
            {
                return;
            }

            _completionPunchTweener.Play();
        }

        /// <summary>
        /// Names whatever is unassigned. Every reference here degrades quietly on its own, so
        /// without this a half-wired bar looks like a broken feature rather than an empty field.
        /// </summary>
        private void LogMissingReferences()
        {
            var missing = new List<string>();

            if (_progressSlider == null) missing.Add(nameof(_progressSlider));
            if (_fruitIconImage == null) missing.Add(nameof(_fruitIconImage));
            if (_progressText == null) missing.Add(nameof(_progressText));
            if (_completedTickmark == null) missing.Add(nameof(_completedTickmark));

            if (missing.Count == 0)
            {
                return;
            }

            Debug.LogError($"[FruitGoalView] Unassigned on {name}: {string.Join(", ", missing)}.", this);
        }
    }
}
