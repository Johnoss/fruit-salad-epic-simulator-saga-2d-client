using System;
using System.Collections.Generic;
using Board;
using Interaction;
using JetBrains.Annotations;
using MVC;
using Tile;
using UniRx;
using UnityEngine;
using Utils;

namespace Boosters
{
    /// <summary>
    /// Drops wildcard tiles onto the board, biased towards the centre and away from each other.
    ///
    /// Centre bias matters because a wildcard is only worth an ad view if the player can actually
    /// build a chain through it, and edge tiles have three neighbours to a centre tile's eight.
    /// </summary>
    [UsedImplicitly]
    public class StarfruitPlacer : MVCDisposable
    {
        private readonly BoardModel _boardModel;
        private readonly TileConfig _tileConfig;
        private readonly TileFactory _tileFactory;
        private readonly BoosterConfig _boosterConfig;
        private readonly SelectionController _selectionController;

        /// <summary>
        /// Squares promised to a Starfruit that is still flying in. Held because placement is now
        /// deferred until the flight lands: without it a second Starfruit from the same batch could
        /// pick a square the first one is already on its way to.
        /// </summary>
        private readonly HashSet<Vector2Int> _reservedCoordinates = new();

        /// Raised with the square a Starfruit is about to land on. A view flies a sprite there and
        /// calls CommitStarfruit on arrival.
        public readonly Subject<Vector2Int> OnStarfruitIncoming = new();

        /// Raised as each Starfruit actually lands, whatever asked for it - a reward or the fruit
        /// goal. Sound hangs off this rather than off the grant, so it matches what is on screen.
        public readonly Subject<Vector2Int> OnStarfruitLanded = new();

        public StarfruitPlacer(BoardModel boardModel, TileConfig tileConfig,
            TileFactory tileFactory, BoosterConfig boosterConfig, SelectionController selectionController)
        {
            _boardModel = boardModel;
            _tileConfig = tileConfig;
            _tileFactory = tileFactory;
            _boosterConfig = boosterConfig;
            _selectionController = selectionController;
        }

        /// <summary>
        /// Drops <paramref name="count"/> Starfruits onto a board that has just been built, as part
        /// of its starting layout.
        ///
        /// Neither staggered nor flown, unlike the rewarded batch: at level start there is nothing
        /// on screen for them to fly from, and they are meant to read as how the board was dealt
        /// rather than as something arriving.
        /// </summary>
        public void PlaceStartingStarfruits(int count)
        {
            if (count <= 0)
            {
                return;
            }

            if (_tileConfig.WildcardTileSetting == null)
            {
                Debug.LogError("[StarfruitPlacer] No tile setting in TileConfig is flagged IsWildcard, " +
                               "so a level asking for starting Starfruits gets none.");
                return;
            }

            for (var placed = 0; placed < count; placed++)
            {
                if (!TryPickCoordinates(out var coordinates))
                {
                    Debug.LogWarning($"[StarfruitPlacer] Ran out of room after {placed} of {count} " +
                                     "starting Starfruits.");
                    return;
                }

                CommitStarfruit(coordinates);
            }
        }

        /// <summary>
        /// Schedules <paramref name="count"/> Starfruit tiles, each landing after its own random
        /// delay so a batch scatters in rather than appearing at once. Returns whether the reward
        /// could be started at all.
        /// </summary>
        public bool TryPlaceStarfruits(int count)
        {
            var wildcardSetting = _tileConfig.WildcardTileSetting;

            if (wildcardSetting == null)
            {
                Debug.LogError("[StarfruitPlacer] No tile setting in TileConfig is flagged IsWildcard, " +
                               "so there is no Starfruit to place.");
                return false;
            }

            if (count <= 0)
            {
                return false;
            }

            // Checked up front because placement is staggered: once the timers are scheduled there
            // is no synchronous answer left to give the caller about whether the reward landed.
            if (!HasAnyValidTarget())
            {
                Debug.LogWarning("[StarfruitPlacer] Found no valid node to place a Starfruit on.");
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                ScheduleStarfruit();
            }

            return true;
        }

        private void ScheduleStarfruit()
        {
            var maxDelay = Mathf.Max(0f, _boosterConfig.StarfruitSpawnDelayMax);
            var delaySeconds = (float)RandomUtils.Rand.NextDouble() * maxDelay;

            if (delaySeconds <= 0f)
            {
                PlaceStarfruit();
                return;
            }

            Observable
                .Timer(TimeSpan.FromSeconds(delaySeconds))
                .SubscribeBlind(PlaceStarfruit)
                .AddTo(Disposer);
        }

        private void PlaceStarfruit()
        {
            // The target is picked here rather than when the batch was scheduled. The board moves
            // between spawns, and picking late is what lets each Starfruit avoid both the player's
            // chain and the siblings that landed ahead of it.
            if (!TryPickCoordinates(out var coordinates))
            {
                return;
            }

            // With no flight view listening there is nothing to wait for, so it lands right away.
            // The Starfruit is the reward - it can never depend on the effect being wired up.
            if (!OnStarfruitIncoming.HasObservers)
            {
                CommitStarfruit(coordinates);
                return;
            }

            _reservedCoordinates.Add(coordinates);
            OnStarfruitIncoming.OnNext(coordinates);
        }

        /// <summary>
        /// Lands a reserved Starfruit. Called when its flight arrives.
        /// </summary>
        public void CommitStarfruit(Vector2Int coordinates)
        {
            _reservedCoordinates.Remove(coordinates);

            var wildcardSetting = _tileConfig.WildcardTileSetting;

            if (wildcardSetting == null)
            {
                return;
            }

            // The flight already committed to this square, so it takes the tile over even if the
            // player has chained it in the meantime. Dropping it from the selection first keeps the
            // chain from holding a disposed tile and eating the Starfruit on release.
            var occupyingTile = _boardModel.GetTileByCoordinates(coordinates);

            if (occupyingTile != null && occupyingTile.Model.IsSelected.Value)
            {
                _selectionController.DropSelectionAt(coordinates);
            }

            var node = _boardModel.GetNodeByCoordinates(coordinates);
            var starfruit = _tileFactory.CreateTileContainer(wildcardSetting, node.Model);

            node.Controller.ReplaceTile(starfruit);

            OnStarfruitLanded.OnNext(coordinates);
        }

        private bool HasAnyValidTarget()
        {
            foreach (var node in _boardModel.Nodes)
            {
                if (IsValidTarget(node.Model.Coordinates))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryPickCoordinates(out Vector2Int coordinates)
        {
            var centre = GetBoardCentre();
            var spread = Mathf.Max(0.01f, _boosterConfig.StarfruitSpread);

            // Two passes rather than one hard rule. Clustering wastes the booster - a wildcard next
            // to a wildcard adds far less reach than one in fresh territory - but on a crowded board
            // placing adjacent still beats placing nothing, so it is a preference, not a ban.
            if (TrySampleCoordinates(centre, spread, true, out coordinates))
            {
                return true;
            }

            if (TrySampleCoordinates(centre, spread, false, out coordinates))
            {
                return true;
            }

            return TryGetBestValidNode(centre, out coordinates);
        }

        private bool TrySampleCoordinates(Vector2 centre, float spread, bool avoidWildcardNeighbours,
            out Vector2Int coordinates)
        {
            coordinates = default;

            for (var attempt = 0; attempt < _boosterConfig.StarfruitPlacementAttempts; attempt++)
            {
                var candidate = new Vector2Int(
                    Mathf.RoundToInt(RandomUtils.NextGaussian(centre.x, spread)),
                    Mathf.RoundToInt(RandomUtils.NextGaussian(centre.y, spread)));

                // Resample rather than clamp. Clamping would fold the whole outer tail onto the
                // border and make edge placements more likely than the ones just inside them,
                // which is the opposite of the intended bias.
                if (!_boardModel.IsWithinGrid(candidate) || !IsValidTarget(candidate))
                {
                    continue;
                }

                if (avoidWildcardNeighbours && HasWildcardNeighbour(candidate))
                {
                    continue;
                }

                coordinates = candidate;
                return true;
            }

            return false;
        }

        private Vector2 GetBoardCentre()
        {
            return new Vector2(
                (_boardModel.GridResolution.Value.x - 1) * 0.5f,
                (_boardModel.GridResolution.Value.y - 1) * 0.5f);
        }

        private bool IsValidTarget(Vector2Int coordinates)
        {
            if (_reservedCoordinates.Contains(coordinates))
            {
                return false;
            }

            var tile = _boardModel.GetTileByCoordinates(coordinates);

            // Skip tiles mid-chain (replacing one under the player's finger desyncs the selection)
            // and tiles that are already wildcards (no value in paying to overwrite one).
            return tile != null && !tile.IsWildcard && !tile.Model.IsSelected.Value;
        }

        /// Adjacency here is the same eight-neighbourhood the chain rules use, diagonals included.
        private bool HasWildcardNeighbour(Vector2Int coordinates)
        {
            foreach (var neighbour in coordinates.GetAdjacentCoordinates(_boardModel.GridResolution.Value))
            {
                var tile = _boardModel.GetTileByCoordinates(neighbour);

                if (tile != null && tile.IsWildcard)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Deterministic fallback for when sampling keeps missing. Isolation outranks proximity,
        /// so distance to centre only breaks ties between candidates of the same kind.
        /// </summary>
        private bool TryGetBestValidNode(Vector2 centre, out Vector2Int coordinates)
        {
            coordinates = default;

            var found = false;
            var bestIsIsolated = false;
            var bestDistance = float.MaxValue;

            foreach (var node in _boardModel.Nodes)
            {
                var candidate = node.Model.Coordinates;

                if (!IsValidTarget(candidate))
                {
                    continue;
                }

                var isIsolated = !HasWildcardNeighbour(candidate);
                var distance = Vector2.SqrMagnitude(new Vector2(candidate.x, candidate.y) - centre);

                if (found)
                {
                    if (isIsolated != bestIsIsolated)
                    {
                        if (!isIsolated)
                        {
                            continue;
                        }
                    }
                    else if (distance >= bestDistance)
                    {
                        continue;
                    }
                }

                found = true;
                bestIsIsolated = isIsolated;
                bestDistance = distance;
                coordinates = candidate;
            }

            return found;
        }
    }
}
