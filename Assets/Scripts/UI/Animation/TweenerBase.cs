using DG.Tweening;
using UnityEngine;

namespace UI.Animation
{
    public abstract class TweenerBase<TTweenTarget> : MonoBehaviour
    where TTweenTarget : Component
    {
        protected Tween Tweener;
        
        [SerializeField]
        protected TTweenTarget TargetComponent;

        public virtual void KillTween()
        {
            Tweener?.Kill();
        }
    }
}