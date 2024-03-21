using MVC;
using Tile;
using UnityEngine;
using Utils;
using Zenject;

namespace Installers
{
    public class TileInstaller : MonoInstaller
    {
        [Impject]
        private TileConfig _tileConfig;

        [SerializeField]
        private RectTransform _tilesParent;

        public override void InstallBindings()
        {
            Application.targetFrameRate = 120;
            Container.Bind<TileFactory>().AsSingle();

            Container.Bind<ViewPool<TileView>>().AsSingle();
            
            Container.BindFactory<ViewPool<TileView>, TileView, TileView.ViewFactory>()
                .FromComponentInNewPrefab(_tileConfig.TilePrefab)
                .UnderTransform(_tilesParent).AsSingle().NonLazy();
        }
    }
}