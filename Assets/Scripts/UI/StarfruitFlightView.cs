using Board;
using Boosters;
using DG.Tweening;
using MVC;
using Tile;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Zenject;

namespace UI
{
    /// <summary>
    /// Flies a Starfruit from its icon to the square it is about to land on. The tile is only
    /// replaced when the flight arrives, so the sprite reads as the thing being delivered rather
    /// than as decoration over an instant spawn.
    /// </summary>
    public class StarfruitFlightView : AbstractView, IInitializable
    {
        [Header("Launch point")]
        [SerializeField]
        [Tooltip("Where the Starfruit flies from, normally the Starfruit reward button's icon")]
        private RectTransform _starfruitIcon;

        [Header("Flight")]
        [SerializeField]
        [Tooltip("Parent for the flying sprite, normally the UI canvas root so it draws over the board")]
        private RectTransform _flightParent;

        [SerializeField]
        [Tooltip("Prefab with an Image on its root")]
        private Image _flyingStarfruitPrefab;

        [SerializeField]
        [Tooltip("Fixed, unlike the fruit goal's speed-based flight. It is short enough that the " +
                 "distance does not read, and a predictable delay before the tile changes.")]
        private float _flightSeconds = 0.25f;

        [SerializeField]
        private Ease _flightEase = Ease.InOutQuad;

        [Impject]
        private StarfruitPlacer _starfruitPlacer;

        [Impject]
        private BoardModel _boardModel;

        [Impject]
        private TileConfig _tileConfig;

        private FlyingSpritePool _flyingSpritePool;

        [Impject]
        public void Initialize()
        {
            _flyingSpritePool = new FlyingSpritePool(_flyingStarfruitPrefab, _flightParent);

            _starfruitPlacer.OnStarfruitIncoming.Subscribe(SendStarfruit).AddTo(Disposer);
        }

        private void SendStarfruit(Vector2Int coordinates)
        {
            // Nodes outlive their tiles, so this stays valid for the whole flight even if the tile
            // currently sitting there is collected on the way.
            var targetPosition = _boardModel.GetTileParentByCoordinates(coordinates).position;
            var fromPosition = _starfruitIcon != null ? _starfruitIcon.position : targetPosition;

            var wildcardSetting = _tileConfig.WildcardTileSetting;

            _flyingSpritePool.Launch(
                wildcardSetting?.Sprite,
                fromPosition,
                targetPosition,
                _flightSeconds,
                _flightEase,
                _boardModel.NodeSideLength.Value,
                () => _starfruitPlacer.CommitStarfruit(coordinates));
        }
    }
}
