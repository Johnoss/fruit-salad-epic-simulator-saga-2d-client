using JetBrains.Annotations;
using MVC;
using UniRx;
using UnityEngine;

namespace Interaction
{
    [UsedImplicitly]
    public class InputController : AbstractController
    {
        private readonly InputModel _model;

        public InputController(InputModel model)
        {
            _model = model;

            Observable.EveryUpdate()
                .Select(_ => Input.touchCount > 0 || Input.GetMouseButton(0))
                .Subscribe(UpdateInput);
        }

        private void UpdateInput(bool isInteracting)
        {
            _model.SetIsInteracting(isInteracting);
        }
    }
}