using Interaction;
using JetBrains.Annotations;
using MVC;
using UniRx;

namespace Score
{
    [UsedImplicitly]
    public class GoalController : AbstractController
    {
        private readonly GoalModel _model;

        public GoalController(GoalModel model, SelectionModel selectionModel)
        {
            _model = model;

            selectionModel.OnCollect.Subscribe(model.UpdateCollected).AddTo(Disposer);
        }

        public void RestartGoal()
        {
            _model.SetCollected(0);
        }
    }
}