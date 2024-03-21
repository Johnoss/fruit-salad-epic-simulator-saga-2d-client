using JetBrains.Annotations;
using MVC;
using Tile;
using UniRx;

namespace Interaction
{
    [UsedImplicitly]
    public class SelectionModel : AbstractModel
    {
        private readonly IReactiveProperty<TileType> _selectedTileType = new ReactiveProperty<TileType>();
        public IReadOnlyReactiveProperty<TileType> SelectedTileType => _selectedTileType;

        private readonly IReactiveCollection<TileContainer> _selectedTiles = new ReactiveCollection<TileContainer>();
        public IReadOnlyReactiveCollection<TileContainer> SelectedTiles => _selectedTiles;

        public readonly ISubject<int> OnCollect = new Subject<int>();

        public void AddSelectedTile(TileContainer tile)
        {
            _selectedTiles.Add(tile);
        }

        public void SetSelectedType(TileType type)
        {
            _selectedTileType.Value = type;
        }

        public void ClearSelectedType()
        {
            SetSelectedType(TileType.None);
        }

        public void RemoveSelectedTile(int index)
        {
            _selectedTiles.RemoveAt(index);
        }

        public void Collect(int collectAmount)
        {
            OnCollect.OnNext(collectAmount);
        }
    }
}