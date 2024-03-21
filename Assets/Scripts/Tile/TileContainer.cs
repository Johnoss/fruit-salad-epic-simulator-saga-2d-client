using System;
using UniRx;

namespace Tile
{
    public class TileContainer : IDisposable
    {
        public TileModel Model { get; }
        public TileController Controller { get; }

        public TileType TileType { get; }
        
        private readonly CompositeDisposable _tileDisposer;
        
        public TileContainer(TileModel model, TileController controller, CompositeDisposable tileDisposer)
        {
            _tileDisposer = tileDisposer;
            Model = model.AddTo(tileDisposer);
            Controller = controller.AddTo(tileDisposer);

            TileType = Model.TileSetting.TileType;
        }

        public void Dispose()
        {
            _tileDisposer.Dispose();
        }
    }
}