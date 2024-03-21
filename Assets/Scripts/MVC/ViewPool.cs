using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Zenject;

namespace MVC
{
    [UsedImplicitly]
    public class ViewPool<TView>
        where TView : AbstractView, IPoolableView
    {
        private readonly Queue<TView> _views = new();

        private IFactory<ViewPool<TView>, TView> _viewFactory;

        public void SetupPool(int initialViewCount, IFactory<ViewPool<TView>, TView> viewFactory)
        {
            _viewFactory = viewFactory;
            
            for (var i = 0; i < initialViewCount; i++)
            {
                AddView(_viewFactory.Create(this));
            }
        }

        public TView GetPooledOrNewView()
        {
            var view = _views.Any() ? _views.Dequeue() : _viewFactory.Create(this);
            view.ResetView();
            return view;
        }

        public void AddView(TView view)
        {
            view.DisableView();
            _views.Enqueue(view);
        }
    }
}