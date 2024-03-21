using System;
using Interaction;
using MVC;
using Score;
using Tile;
using TMPro;
using UI.Animation;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Zenject;

namespace UI
{
    public class CurrentChainView : AbstractView, IInitializable
    {
        [Header("Current Chain")]
        [SerializeField]
        private TextMeshProUGUI _resultScoreText;
        [SerializeField]
        private TextMeshProUGUI _chainLengthText;
        [SerializeField]
        private Image _tileTypeImage;

        [Header("Layout")]
        [SerializeField]
        private GameObject _contentContainer;
        
        // [Header("Visuals")]
        // [SerializeField]
        // private AnchorPositionTweener _slideTween;
        // [SerializeField]
        // private float _hideAfterSecondsInactive;
        
        [Impject]
        private SelectionModel _selectionModel;
        
        [Impject]
        private ScoreController _scoreController;

        [Impject]
        private TileConfig _tileConfig;
        
        [Impject]
        public void Initialize()
        {
            _selectionModel.SelectedTiles
                .ObserveCountChanged()
                .Subscribe(UpdateChainLength).AddTo(Disposer);

            _selectionModel.SelectedTileType.Subscribe(UpdateCurrentType).AddTo(Disposer);

            var isChainActiveObservable = _selectionModel.SelectedTiles.ObserveCountChanged()
                .Select(chainLength => chainLength > 0).ToReactiveProperty();
            
            isChainActiveObservable
                .Subscribe(TogglePanel).AddTo(Disposer);
            
            // isChainActiveObservable
            //     .DistinctUntilChanged()
            //     .Throttle(TimeSpan.FromSeconds(_hideAfterSecondsInactive))
            //     .WhereFalse()
            //     .Subscribe(_ => TogglePanel(false)).AddTo(Disposer);
        }

        private void UpdateChainLength(int chainLength)
        {
            _contentContainer.SetActive(chainLength > 0);
            
            _resultScoreText.text = $"+{_scoreController.GetScoreForChain(chainLength):F0}";
            _chainLengthText.text = $"x{chainLength}";
        }

        private void UpdateCurrentType(TileType type)
        {
            _tileTypeImage.overrideSprite = _tileConfig.GetTileSpriteOrDefault(type);
        }

        private void TogglePanel(bool isActive)
        {
            _contentContainer.SetActive(isActive);
        }
    }
}