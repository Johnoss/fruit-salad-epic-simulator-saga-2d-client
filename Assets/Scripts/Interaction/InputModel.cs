using JetBrains.Annotations;
using MVC;
using UniRx;
using Utils;

namespace Interaction
{
    [UsedImplicitly]
    public class InputModel : AbstractModel
    {
        private readonly IReactiveProperty<bool> _isInteracting = new ReactiveProperty<bool>();
        public IReadOnlyReactiveProperty<bool> IsInteracting => _isInteracting;

        public void SetIsInteracting(bool isInteracting)
        {
            _isInteracting.Value = isInteracting;
        }
    }
}