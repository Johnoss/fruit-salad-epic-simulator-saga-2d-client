using System.Collections.Generic;
using System.Linq;
using Interaction;
using JetBrains.Annotations;
using MVC;
using Node;
using Score;
using UniRx;
using UnityEngine;
using Utils;

namespace Board
{
    [UsedImplicitly]
    public class BoardController : AbstractController
    {
        /// <summary>
        /// How many times a board may be reshuffled looking for a legal move before we give up.
        /// A level with too much fruit for its size can genuinely fail this every time, and the
        /// reshuffle used to recurse - so that authoring mistake was a hang rather than a message.
        /// </summary>
        private const int MaxReshuffleAttempts = 20;

        private readonly NodeFactory _nodeFactory;

        private readonly BoardConfig _boardConfig;
        private readonly ScoreConfig _scoreConfig;

        private readonly BoardModel _model;

        public BoardController(NodeFactory nodeFactory, BoardConfig boardConfig, ScoreConfig scoreConfig,
            BoardModel model, SelectionModel selectionModel)
        {
            _nodeFactory = nodeFactory;
            _boardConfig = boardConfig;
            _scoreConfig = scoreConfig;
            _model = model;

            selectionModel.OnCollect.DelayFrame(1).SubscribeBlind(ValidateMovesExist).AddTo(Disposer);
        }

        /// <summary>
        /// Builds a board at <paramref name="gridResolution"/>, throwing away whatever was there.
        /// This is the level-start entry point: a level that wants a different shape says so here
        /// and everything downstream resizes off the model.
        /// </summary>
        public void StartBoard(Vector2Int gridResolution)
        {
            if (gridResolution.x <= 0 || gridResolution.y <= 0)
            {
                Debug.LogError($"[BoardController] Refusing to build a {gridResolution} board. " +
                               "Keeping the board that is already up.");
                return;
            }

            DisposeNodes();

            _model.RebuildNodes(gridResolution);

            PopulateBoard();
            ValidateMovesExist();
        }

        public void SetNodeSideLength(float length)
        {
            _model.SetNodeSideLength(length);
        }

        /// Clears every tile, leaving the nodes in place. The refill is automatic.
        public void RepopulateBoard()
        {
            ClearTiles();

            ValidateMovesExist();
        }

        private void ClearTiles()
        {
            foreach (var nodeContainer in _model.Nodes)
            {
                nodeContainer.Controller.RemoveTile();
            }
        }

        /// <summary>
        /// Tears down the outgoing board. Controllers go first: they listen for their node's tile
        /// going away and would start hauling tiles down a board that is being deleted.
        /// </summary>
        private void DisposeNodes()
        {
            foreach (var nodeContainer in _model.Nodes)
            {
                nodeContainer?.Dispose();
            }
        }

        private void PopulateBoard()
        {
            var gridResolution = _model.GridResolution.Value;

            for (var row = 0; row < gridResolution.x; row++)
            {
                for (var column = 0; column < gridResolution.y; column++)
                {
                    var coordinates = new Vector2Int(row, column);
                    _model.AddNode(_nodeFactory.CreateNode(coordinates));
                }
            }
        }

        private void ValidateMovesExist()
        {
            for (var attempt = 0; attempt < MaxReshuffleAttempts; attempt++)
            {
                if (HasAnyMove())
                {
                    return;
                }

                ClearTiles();
            }

            Debug.LogError($"[BoardController] No legal move after {MaxReshuffleAttempts} reshuffles " +
                           $"on a {_model.GridResolution.Value} board. The level is almost certainly " +
                           "asking for more fruit than a board this size can hold a match of.");
        }

        private bool HasAnyMove()
        {
            var refuseCoordinates = new HashSet<Vector2Int>();

            foreach (var node in _model.Nodes)
            {
                var coordinate = node.Model.Coordinates;
                if (refuseCoordinates.Contains(coordinate))
                {
                    continue;
                }

                var currentRefuseCoordinates = new HashSet<Vector2Int>(refuseCoordinates);

                var matches = GetMatchingNeighboursRecursively(coordinate, currentRefuseCoordinates, new List<Vector2Int>(),
                    _scoreConfig.MinChain);
                if (matches.Count >= _scoreConfig.MinChain)
                {
                    return true;
                }

                refuseCoordinates.Add(coordinate);
            }

            return false;
        }

        private List<Vector2Int> GetMatchingNeighboursRecursively(Vector2Int originCoordinate,
            HashSet<Vector2Int> refuseCoordinates, List<Vector2Int> matchingNeighbours, int maxRecursion = int.MaxValue)
        {
            if (matchingNeighbours.Count >= maxRecursion)
            {
                return matchingNeighbours;
            }
            var adjacentCoordinates =
                originCoordinate.GetAdjacentCoordinates(_model.GridResolution.Value, refuseCoordinates);

            foreach (var neighbour in adjacentCoordinates.Where(neighbour => CheckTypeMatches(originCoordinate, neighbour)))
            {
                refuseCoordinates.Add(neighbour);
                matchingNeighbours.Add(neighbour);
                GetMatchingNeighboursRecursively(neighbour, refuseCoordinates, matchingNeighbours);
            }

            return matchingNeighbours;
        }

        private bool CheckTypeMatches(Vector2Int coordinate1, Vector2Int coordinate2)
        {
            var tile1 = _model.GetTileByCoordinates(coordinate1);
            var tile2 = _model.GetTileByCoordinates(coordinate2);

            if (tile1 == null || tile2 == null)
            {
                return false;
            }

            // Wildcards have to match here too, not just in SelectionController. Otherwise a board
            // holding a paid-for Starfruit can read as "no moves left" and get wiped by
            // RepopulateBoard, destroying the booster the player just watched an ad for.
            if (tile1.IsWildcard || tile2.IsWildcard)
            {
                return true;
            }

            return tile1.TileType == tile2.TileType;
        }
    }
}
