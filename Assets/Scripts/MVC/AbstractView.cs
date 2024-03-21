using System;
using UniRx;
using UnityEngine;

namespace MVC
{
    public abstract class AbstractView : MonoBehaviour, IDisposable
    {
        protected CompositeDisposable Disposer = new();

        public void SetDisposer(CompositeDisposable disposer)
        {
            Disposer.AddTo(disposer);
            Disposer = disposer;
            Disposer.Add(this);
        }

        public virtual void Dispose()
        {
            Disposer?.Dispose();
            Destroy(gameObject);
        }
    }
}