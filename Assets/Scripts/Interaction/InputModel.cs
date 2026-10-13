using System.Collections.Generic;
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

        private readonly HashSet<InputBlockReason> _blockReasons = new();
        private readonly IReactiveProperty<bool> _isBlocked = new ReactiveProperty<bool>();

        /// <summary>
        /// Suppresses board input without touching timeScale. True while any reason is active;
        /// this is also where the SDK's onPause will hook in.
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsBlocked => _isBlocked;

        public void SetIsInteracting(bool isInteracting)
        {
            _isInteracting.Value = isInteracting;
        }

        public void SetIsBlocked(InputBlockReason reason, bool isBlocked)
        {
            if (isBlocked)
            {
                _blockReasons.Add(reason);
            }
            else
            {
                _blockReasons.Remove(reason);
            }

            _isBlocked.Value = _blockReasons.Count > 0;
        }
    }
}