using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MVC;
using Tile;
using UniRx;

namespace Level
{
    [UsedImplicitly]
    public class LevelModel : AbstractModel
    {
        private readonly IReactiveProperty<int> _currentLevelIndex = new ReactiveProperty<int>(-1);

        /// Zero based. The level currently built on the board; -1 before the first one starts.
        public IReadOnlyReactiveProperty<int> CurrentLevelIndex => _currentLevelIndex;

        private readonly IReactiveProperty<int> _previewLevelIndex = new ReactiveProperty<int>(0);

        /// <summary>
        /// The level the preview panel is advertising, which is not always the one on the board:
        /// finishing a level leaves the finished board up and points the preview at the next one.
        /// </summary>
        public IReadOnlyReactiveProperty<int> PreviewLevelIndex => _previewLevelIndex;

        private readonly IReactiveProperty<LevelDefinition> _previewLevel =
            new ReactiveProperty<LevelDefinition>();

        /// The definition behind <see cref="PreviewLevelIndex"/>, so views need not index the set.
        public IReadOnlyReactiveProperty<LevelDefinition> PreviewLevel => _previewLevel;

        /// <summary>
        /// What the board spawns on the current level, already resolved against TileConfig. Empty
        /// means the level did not name any and the full spawn pool applies.
        /// </summary>
        public IReadOnlyList<TileSetting> SpawnableTileSettings { get; private set; } =
            Array.Empty<TileSetting>();

        private readonly IReactiveProperty<bool> _isContentComplete = new ReactiveProperty<bool>();

        /// <summary>
        /// Whether the player has finished the last level. Sticky once set: the game says there is
        /// no more content from then on, including across a replay and across a restart.
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsContentComplete => _isContentComplete;

        public void SetIsContentComplete(bool isContentComplete)
        {
            _isContentComplete.Value = isContentComplete;
        }

        public void SetCurrentLevelIndex(int currentLevelIndex)
        {
            _currentLevelIndex.Value = currentLevelIndex;
        }

        public void SetPreviewLevel(int previewLevelIndex, LevelDefinition previewLevel)
        {
            _previewLevelIndex.Value = previewLevelIndex;
            _previewLevel.Value = previewLevel;
        }

        public void SetSpawnableTileSettings(IReadOnlyList<TileSetting> spawnableTileSettings)
        {
            SpawnableTileSettings = spawnableTileSettings ?? Array.Empty<TileSetting>();
        }
    }
}
