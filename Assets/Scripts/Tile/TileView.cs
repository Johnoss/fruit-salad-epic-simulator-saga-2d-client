using System.Linq;
using Board;
using Interaction;
using JetBrains.Annotations;
using MVC;
using UI.Animation;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Zenject;

namespace Tile
{
    public class TileView : AbstractView, IPoolableView
    {
        [UsedImplicitly]
        public class ViewFactory : PlaceholderFactory<ViewPool<TileView>, TileView>
        {
        }

        [Header("Visuals")]
        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private Canvas _tileCanvas;

        [Header("Animations")]
        [SerializeField]
        private WobbleTweener _chainedTweener;

        [SerializeField]
        private AnchorPositionTweener _reparentTweener;

        [SerializeField]
        private FadeTweener _createTileTweener;

        [SerializeField]
        private RippleTweener _rippleTweener;


        private TileModel _model;
        private BoardModel _boardModel;
        private SelectionModel _selectionModel;

        private ViewPool<TileView> _viewPool;
        private RectTransform _rectTransform;

        [Impject]
        public void Construct(ViewPool<TileView> viewPool)
        {
            _viewPool = viewPool;
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Setup(TileModel model, BoardModel boardModel, SelectionModel selectionModel)
        {
            _iconImage.sprite = model.TileSetting.Sprite;

            _model = model;
            _boardModel = boardModel;
            _selectionModel = selectionModel;

            _model.IsSelected.Subscribe(UpdateIsSelected).AddTo(Disposer);
            _model.Coordinates.Subscribe(UpdatePosition).AddTo(Disposer);

            
            _selectionModel.OnCollect
                .SkipUntil(_selectionModel.SelectedTileType)
                .SubscribeBlind(PlayRippleTween)
                .AddTo(Disposer);

            _boardModel.NodeSideLength.Subscribe(UpdateTileSize).AddTo(Disposer);
            
            SetupLayout();
            
            _createTileTweener.Play();
        }

        private void UpdatePosition(Vector2Int coordinates)
        {
            _tileCanvas.sortingOrder = -coordinates.y;
            var newParent = _boardModel.GetTileParentByCoordinates(coordinates);

            transform.SetParent(newParent);

            _reparentTweener.Play(Vector2.zero);
        }

        private void UpdateIsSelected(bool isSelected)
        {
            _chainedTweener.TogglePlay(!isSelected);
        }

        private void UpdateTileSize(float length)
        {
            _rectTransform.sizeDelta = Vector2.one * length;
        }

        private void SetupLayout()
        {
            var nodeHeight = _boardModel.NodeSideLength.Value;
            _rectTransform.localPosition =
                _boardModel.GetTileParentByCoordinates(_model.Coordinates.Value).localPosition;
            _rectTransform.anchoredPosition += Vector2.up * nodeHeight;
        }

        private void PlayRippleTween()
        {
            if (!_selectionModel.SelectedTiles.Any())
            {
                return;
            }

            var lastSelectedTilePosition = _selectionModel.SelectedTiles.Last().Model.Coordinates.Value;
            var distanceFromLastSelected = Vector2Int.Distance(lastSelectedTilePosition, _model.Coordinates.Value);
            _rippleTweener.Play(distanceFromLastSelected);
        }

        public override void Dispose()
        {
            _viewPool.AddView(this);
        }

        public void ResetView()
        {
            _chainedTweener.KillTween();
            _reparentTweener.KillTween();
            _createTileTweener.KillTween();
            _rippleTweener.KillTween();

            gameObject.SetActive(true);
        }

        public void DisableView()
        {
            gameObject.SetActive(false);
        }
    }
}