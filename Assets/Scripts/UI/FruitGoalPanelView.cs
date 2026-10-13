using System.Collections.Generic;
using FruitGoal;
using MVC;
using Tile;
using UniRx;
using UnityEngine;
using Utils;
using Zenject;

namespace UI
{
    /// <summary>
    /// Points the objective bars at the level's objectives and switches off the ones the level does
    /// not use.
    ///
    /// Bars are assigned in list order, so the order they are dragged in is the order they read on
    /// screen. Unused ones are disabled rather than blanked, which is what lets the layout group
    /// close the gap instead of leaving a hole.
    /// </summary>
    public class FruitGoalPanelView : AbstractView, IInitializable
    {
        [Header("Bars")]
        [SerializeField]
        [Tooltip("One per objective a level may have, top to bottom. A level with fewer objectives " +
                 "disables the extras.")]
        private List<FruitGoalView> _goalViews = new();

        [Impject]
        private FruitGoalModel _model;

        [Impject]
        private TileConfig _tileConfig;

        [Impject]
        public void Initialize()
        {
            if (_goalViews.Count == 0)
            {
                Debug.LogError("[FruitGoalPanelView] No objective bars assigned, so the level's " +
                               "objectives have nowhere to show.", this);
            }

            _model.Objectives.Subscribe(BindObjectives).AddTo(Disposer);
        }

        private void BindObjectives(IReadOnlyList<FruitObjective> objectives)
        {
            if (objectives.Count > _goalViews.Count)
            {
                Debug.LogError($"[FruitGoalPanelView] {objectives.Count} objectives but only " +
                               $"{_goalViews.Count} bars. The extra objectives still have to be " +
                               "completed, but the player cannot see them.", this);
            }

            for (var index = 0; index < _goalViews.Count; index++)
            {
                var goalView = _goalViews[index];

                if (goalView == null)
                {
                    Debug.LogError($"[FruitGoalPanelView] Bar {index} is unassigned.", this);
                    continue;
                }

                if (index >= objectives.Count)
                {
                    goalView.Hide();
                    continue;
                }

                var objective = objectives[index];

                goalView.Bind(objective, _tileConfig.GetTileSpriteOrDefault(objective.Fruit));
            }
        }
    }
}
