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
        private Transform _poolParent;

        [Impject]
        public void Construct(ViewPool<TileView> viewPool)
        {
            _viewPool = viewPool;
            _rectTransform = GetComponent<RectTransform>();

            // Where the factory put it, captured before the tile ever moves. Tiles reparent to
            // whichever node they sit on, so a pooled one is still a child of a node - and
            // rebuilding the board destroys that node and takes the pooled tile with it.
            _poolParent = transform.parent;
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
            // Cleared, not disposed: the view is going back in the pool and Setup will subscribe it
            // again. Without this every reuse stacked another set of handlers on the long-lived
            // models - OnCollect and NodeSideLength outlive any one tile - and a recycled view
            // ended up rippling once per life it had ever had.
            Disposer.Clear();

            if (_poolParent != null)
            {
                transform.SetParent(_poolParent, false);
            }

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