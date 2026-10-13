using System;
using Board;
using DG.Tweening;
using FruitGoal;
using Interaction;
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
    /// Flies collected fruit the objectives still want into the bowl, and tells them when one
    /// lands.
    ///
    /// Only wanted fruit flies. Anything else - a fruit no objective asks for, or one whose
    /// objective is already full - just leaves the board, so the bowl reads as the level's progress
    /// rather than as everything the player has ever matched.
    ///
    /// The whole chain is judged before any of it lands, so a match that overfills an objective
    /// sends its surplus too. That is deliberate: the alternative is fruit vanishing mid-flight the
    /// instant the bar fills, which looks like a dropped frame rather than a rule.
    /// </summary>
    public class CollectedFruitFlightView : AbstractView, IInitializable
    {
        [Header("Target")]
        [SerializeField]
        [Tooltip("The bowl the fruit flies into, normally empty_bowl_container")]
        private RectTransform _bowlTransform;

        [Header("Flight")]
        [SerializeField]
        [Tooltip("Parent for the flying sprites, normally the UI canvas root so they draw over the board")]
        private RectTransform _flightParent;

        [SerializeField]
        [Tooltip("Prefab with an Image on its root")]
        private Image _flyingFruitPrefab;

        [SerializeField]
        [Tooltip("Screen pixels per second. Duration comes from the distance, so fruit from the far " +
                 "side of the board takes longer to reach the bowl.")]
        private float _flyingSpeed = 1600f;

        [SerializeField]
        private Ease _flyingEase = Ease.InOutQuad;

        [Impject]
        private SelectionModel _selectionModel;

        [Impject]
        private BoardModel _boardModel;

        [Impject]
        private TileConfig _tileConfig;

        // Optional so the scene still boots before FruitGoalInstaller is wired up.
        [InjectOptional]
        private FruitGoalController _fruitGoalController;

        private FlyingSpritePool _flyingSpritePool;

        [Impject]
        public void Initialize()
        {
            _flyingSpritePool = new FlyingSpritePool(_flyingFruitPrefab, _flightParent);

            _selectionModel.OnCollect.SubscribeBlind(SendCollectedFruits).AddTo(Disposer);
        }

        private void SendCollectedFruits()
        {
            // OnCollect fires before the tiles are removed, so the chain is still readable here.
            foreach (var collectedTile in _selectionModel.SelectedTiles)
            {
                SendCollectedFruit(collectedTile);
            }
        }

        private void SendCollectedFruit(TileContainer collectedTile)
        {
            if (!IsWanted(collectedTile))
            {
                return;
            }

            // Read now rather than in the arrival callback. The tile is disposed the moment the
            // chain resolves, long before its sprite lands.
            var onArrived = GetArrivalCallback(collectedTile);

            if (_bowlTransform == null || !_flyingSpritePool.IsUsable)
            {
                // No flight to wait on. The objectives still have to be told, or an unwired bowl
                // would quietly stop every bar from ever advancing.
                onArrived?.Invoke();
                return;
            }

            // Nodes outlive their tiles, so this stays valid though the tile is being removed.
            var fromPosition = _boardModel
                .GetTileParentByCoordinates(collectedTile.Model.Coordinates.Value)
                .position;

            var targetPosition = _bowlTransform.position;

            // Screen pixels, not raw position distance: these canvases are Screen Space Camera, so
            // positions are world units and a px/sec speed would finish the flight instantly.
            var duration = _flyingSpritePool.GetScreenDistance(fromPosition, targetPosition)
                           / Mathf.Max(1f, _flyingSpeed);

            _flyingSpritePool.Launch(
                _tileConfig.GetTileSpriteOrDefault(collectedTile.TileType),
                fromPosition,
                targetPosition,
                duration,
                _flyingEase,
                _boardModel.NodeSideLength.Value,
                onArrived);
        }

        /// <summary>
        /// Whether this tile is worth flying.
        ///
        /// A wildcard reads as its own type, Starfruit, so it can only ever satisfy an objective
        /// that actually asked for Starfruit - never the fruit it was standing in for in the chain.
        ///
        /// With no objectives to ask - the fruit goals are injected optionally - everything flies,
        /// which is the old behaviour and better than a bowl nothing ever reaches.
        /// </summary>
        private bool IsWanted(TileContainer collectedTile)
        {
            return _fruitGoalController == null
                   || _fruitGoalController.WantsFruit(collectedTile.TileType);
        }

        /// What to run when this fruit reaches the bowl.
        private Action GetArrivalCallback(TileContainer collectedTile)
        {
            if (_fruitGoalController == null)
            {
                return null;
            }

            var fruit = collectedTile.TileType;

            return () => _fruitGoalController.RegisterArrival(fruit);
        }
    }
}
