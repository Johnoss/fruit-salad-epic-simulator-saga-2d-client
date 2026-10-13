using System;
using Boosters;
using MVC;

using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Zenject;

namespace UI
{
    public class BoosterView : AbstractView, IInitializable
    {
        [SerializeField]
        private Button _starfruitButton;

        [Header("No-fill feedback (optional)")]
        [SerializeField]
        [Tooltip("Shown briefly when a booster is requested but no ad was served. Optional - " +
                 "leave empty and the request just silently does nothing.")]
        private GameObject _adUnavailableMessage;

        [SerializeField]
        private float _adUnavailableSeconds = 2.5f;

        [Impject]
        private BoosterModel _model;

        [Impject]
        private BoosterController _controller;

        [Impject]
        public void Initialize()
        {
            _starfruitButton.OnClickAsObservable()
                .SubscribeBlind(() => _controller.RequestBooster(BoosterType.Starfruit))
                .AddTo(Disposer);

            _model.IsAdInProgress.SubscribeBlind(UpdateButtonsInteractable).AddTo(Disposer);

            _model.OnBoosterDenied.SubscribeBlind(ShowAdUnavailable).AddTo(Disposer);

            HideAdUnavailable();
        }

        private void UpdateButtonsInteractable()
        {
            _starfruitButton.interactable = _controller.GetCanRequest(BoosterType.Starfruit);
        }

        private void ShowAdUnavailable()
        {
            if (_adUnavailableMessage == null)
            {
                return;
            }

            _adUnavailableMessage.SetActive(true);

            Observable.Timer(TimeSpan.FromSeconds(_adUnavailableSeconds), Scheduler.MainThreadIgnoreTimeScale)
                .SubscribeBlind(HideAdUnavailable)
                .AddTo(Disposer);
        }

        private void HideAdUnavailable()
        {
            if (_adUnavailableMessage == null)
            {
                return;
            }

            _adUnavailableMessage.SetActive(false);
        }
    }
}
