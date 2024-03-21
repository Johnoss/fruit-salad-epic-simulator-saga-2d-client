using Board;
using Interaction;
using MVC;
using Node;
using Score;
using UniRx;
using Utils;
using Zenject;

namespace Tile
{
    public class TileFactory: IInitializable
    {
        [Impject]
        private TileView.ViewFactory _tileViewFactory;

        [Impject]
        private InputModel _inputModel;
        [Impject]
        private BoardModel _boardModel;
        [Impject]
        private SelectionModel _selectionModel;
        [Impject]
        private ScoreModel _scoreModel;
        
        [Impject]
        private SelectionController _selectionController;
        
        [Impject]
        private TileConfig _tileConfig;
        [Impject]
        private BoardConfig _boardConfig;

        [Impject]
        private ViewPool<TileView> _tileViewPool;

        [Impject]
        private DiContainer _container;


        [Impject]
        public void Initialize()
        {
            _tileViewPool.SetupPool(_boardConfig.GridSize, _tileViewFactory);
        }

        public TileContainer CreateRandomTileContainer(NodeModel nodeModel)
        {
            var tileSetting = _tileConfig.TileSettings.RandomElement();
            
            var tileDisposer = new CompositeDisposable();
            
            var model = new TileModel(tileSetting, nodeModel.Coordinates);
            var controller = new TileController(model);

            var view = _tileViewPool.GetPooledOrNewView().AddTo(tileDisposer);
            
            view.Setup(model, _boardModel, _selectionModel);
            
            return new TileContainer(model, controller, tileDisposer).AddTo(tileDisposer);
        }
    }
}