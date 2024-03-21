using System;
using UniRx;

namespace MVC
{
    public abstract class MVCDisposable : IDisposable
    {
        protected readonly CompositeDisposable Disposer = new();

        public virtual void Dispose()
        {
            Disposer?.Dispose();
        }
    }
}