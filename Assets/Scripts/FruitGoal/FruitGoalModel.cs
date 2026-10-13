using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MVC;
using UniRx;

namespace FruitGoal
{
    [UsedImplicitly]
    public class FruitGoalModel : AbstractModel
    {
        /// <summary>
        /// How many objectives a level may have. The panel has one bar per objective and there are
        /// three of them, so a level asking for more would be asking for a bar that does not exist.
        /// </summary>
        public const int MaxObjectivesPerLevel = 3;

        private readonly IReactiveProperty<IReadOnlyList<FruitObjective>> _objectives =
            new ReactiveProperty<IReadOnlyList<FruitObjective>>(Array.Empty<FruitObjective>());

        /// <summary>
        /// The level's objectives, in the order their bars appear. Reactive rather than fixed
        /// because starting a level replaces the whole list, and the panel has to rebind.
        /// </summary>
        public IReadOnlyReactiveProperty<IReadOnlyList<FruitObjective>> Objectives => _objectives;

        private readonly IReactiveProperty<bool> _areAllObjectivesCompleted = new ReactiveProperty<bool>();

        /// <summary>
        /// Whether every objective is done, which is what ends the level. Pushed by the controller
        /// rather than derived here: the list of objectives changes, and combining over a list that
        /// can be swapped out costs more than recomputing it on the handful of events that move it.
        /// </summary>
        public IReadOnlyReactiveProperty<bool> AreAllObjectivesCompleted => _areAllObjectivesCompleted;

        public void SetObjectives(IReadOnlyList<FruitObjective> objectives)
        {
            _objectives.Value = objectives ?? Array.Empty<FruitObjective>();
        }

        public void SetAreAllObjectivesCompleted(bool areAllObjectivesCompleted)
        {
            _areAllObjectivesCompleted.Value = areAllObjectivesCompleted;
        }
    }
}
