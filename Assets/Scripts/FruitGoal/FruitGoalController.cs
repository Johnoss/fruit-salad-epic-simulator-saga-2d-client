using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Level;
using MVC;
using Score;
using Tile;
using UnityEngine;

namespace FruitGoal
{
    /// <summary>
    /// Runs the level's fruit objectives.
    ///
    /// Objectives are all live at once rather than taken one at a time: every fruit that lands in
    /// the bowl is offered to each of them in order, and the first one still wanting that fruit
    /// takes it. Completing them all ends the level.
    ///
    /// Nothing here watches the board. Arrivals come from the flight that carries collected fruit
    /// into the bowl, which is the same flight the player sees, so the bars cannot advance ahead of
    /// what is on screen.
    /// </summary>
    [UsedImplicitly]
    public class FruitGoalController : AbstractController
    {
        private readonly FruitGoalModel _model;
        private readonly FruitGoalConfig _fruitGoalConfig;
        private readonly LevelModel _levelModel;
        private readonly GoalController _goalController;

        private IReadOnlyList<FruitObjective> _objectives = Array.Empty<FruitObjective>();

        public FruitGoalController(FruitGoalModel model, FruitGoalConfig fruitGoalConfig,
            LevelModel levelModel, GoalController goalController)
        {
            _model = model;
            _fruitGoalConfig = fruitGoalConfig;
            _levelModel = levelModel;
            _goalController = goalController;
        }

        /// <summary>
        /// Replaces the live objectives with a fresh set built from <paramref name="definitions"/>.
        /// This is the level-start entry point.
        /// </summary>
        public void StartObjectives(IReadOnlyList<FruitObjectiveDefinition> definitions)
        {
            _objectives = BuildObjectives(definitions);

            _model.SetObjectives(_objectives);

            UpdateCompletion();
        }

        /// <summary>
        /// Whether any objective still wants this fruit. Asked at collect time, before anything
        /// flies, so a fruit nothing is waiting for never leaves the board.
        /// </summary>
        public bool WantsFruit(TileType fruit)
        {
            return _objectives.Any(objective =>
                objective.Fruit == fruit && !objective.IsCompleted.Value);
        }

        /// <summary>
        /// Counts one fruit arriving in the bowl. Called by the flight view when a sprite lands.
        /// </summary>
        public void RegisterArrival(TileType fruit)
        {
            foreach (var objective in _objectives)
            {
                // Offered down the list rather than matched once, so two objectives on the same
                // fruit fill in order instead of the second one never advancing.
                if (objective.Fruit != fruit || !objective.TryCount())
                {
                    continue;
                }

                UpdateCompletion();

                return;
            }
        }

        private IReadOnlyList<FruitObjective> BuildObjectives(
            IReadOnlyList<FruitObjectiveDefinition> definitions)
        {
            if (definitions == null || definitions.Count == 0)
            {
                Debug.LogError("[FruitGoalController] A level with no objectives can never be " +
                               "completed. Nothing will end the level.");

                return Array.Empty<FruitObjective>();
            }

            if (definitions.Count > FruitGoalModel.MaxObjectivesPerLevel)
            {
                Debug.LogError($"[FruitGoalController] {definitions.Count} objectives, but only " +
                               $"{FruitGoalModel.MaxObjectivesPerLevel} bars exist. " +
                               "The extra ones would be invisible and are dropped.");
            }

            var objectives = new List<FruitObjective>();

            foreach (var definition in definitions.Take(FruitGoalModel.MaxObjectivesPerLevel))
            {
                if (definition == null)
                {
                    continue;
                }

                WarnIfUnreachable(definition.Fruit);

                var requiredCount = definition.RequiredCount > 0
                    ? definition.RequiredCount
                    : _fruitGoalConfig.DefaultRequiredCount;

                objectives.Add(new FruitObjective(definition.Fruit, requiredCount));
            }

            return objectives;
        }

        /// <summary>
        /// An objective on a fruit the board never spawns can never be finished, which reads in
        /// play as a level that simply refuses to end. Cheaper to catch here than to sit through.
        /// </summary>
        private void WarnIfUnreachable(TileType fruit)
        {
            // Against the level's own fruit, not the whole tile config: a level can narrow the
            // spawn pool, and asking for a fruit it left out is exactly the mistake to catch.
            if (_levelModel.SpawnableTileSettings.Any(setting => setting.TileType == fruit))
            {
                return;
            }

            Debug.LogError($"[FruitGoalController] Objective wants {fruit}, which this level does " +
                           "not spawn. It can never be collected and the level can never end.");
        }

        private void UpdateCompletion()
        {
            // An empty set is not a finished one. Without this an unconfigured level would declare
            // itself complete on the first frame.
            var areAllCompleted = _objectives.Count > 0
                                  && _objectives.All(objective => objective.IsCompleted.Value);

            _model.SetAreAllObjectivesCompleted(areAllCompleted);

            // The bowl is the level's progress, so it is the objectives added up rather than a
            // count of its own. Surplus is dropped per objective before summing, or a chain that
            // overfilled one bar would push the bowl past full on a level that is not done.
            _goalController.SetBowlProgress(
                _objectives.Sum(objective =>
                    Mathf.Min(objective.CollectedCount.Value, objective.RequiredCount)),
                _objectives.Sum(objective => objective.RequiredCount));

            _goalController.SetIsLevelComplete(areAllCompleted);
        }
    }
}
