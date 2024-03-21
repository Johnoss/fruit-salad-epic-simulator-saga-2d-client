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
        private readonly NodeFactory _nodeFactory;
        
        private readonly BoardConfig _boardConfig;
        private readonly ScoreConfig _scoreConfig;
        
        private readonly BoardModel _model;

        public BoardController(NodeFactory nodeFactory, BoardConfig boardConfig, ScoreConfig scoreConfig, BoardModel model,
            SelectionModel selectionModel)
        {
            _nodeFactory = nodeFactory;
            _boardConfig = boardConfig;
            _scoreConfig = scoreConfig;
            _model = model;

            selectionModel.OnCollect.DelayFrame(1).SubscribeBlind(ValidateMovesExist).AddTo(Disposer);
            
            PopulateBoard();
            ValidateMovesExist();
        }

        public void SetNodeSideLength(float length)
        {
            _model.SetNodeSideLength(length);
        }

        public void RepopulateBoard()
        {
            foreach (var nodeContainer in _model.Nodes)
            {
                nodeContainer.Controller.RemoveTile();
            }
            
            ValidateMovesExist();
        }

        private void PopulateBoard()
        {
            for (var row = 0; row < _boardConfig.GridResolution.x; row++)
            {
                for (var column = 0; column < _boardConfig.GridResolution.y; column++)
                {
                    var coordinates = new Vector2Int(row, column);
                    _model.AddNode(_nodeFactory.CreateNode(coordinates));
                }
            }
        }

        private void ValidateMovesExist()
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
                    return;
                }

                refuseCoordinates.Add(coordinate);
            }

            RepopulateBoard();
        }

        private List<Vector2Int> GetMatchingNeighboursRecursively(Vector2Int originCoordinate,
            HashSet<Vector2Int> refuseCoordinates, List<Vector2Int> matchingNeighbours, int maxRecursion = int.MaxValue)
        {
            if (matchingNeighbours.Count >= maxRecursion)
            {
                return matchingNeighbours;
            }
            var adjacentCoordinates =
                originCoordinate.GetAdjacentCoordinates(_boardConfig.GridResolution, refuseCoordinates);
            
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
            return _model.GetTileByCoordinates(coordinate1).TileType ==
                   _model.GetTileByCoordinates(coordinate2).TileType;
        }
    }
}