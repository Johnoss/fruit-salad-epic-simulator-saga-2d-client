using System.Collections.Generic;
using System.Linq;
using Board;
using Boosters;
using FruitGoal;
using JetBrains.Annotations;
using MVC;
using Platform;
using Save;
using Score;
using Tile;
using UniRx;
using UnityEngine;
using Utils;

namespace Level
{
    /// <summary>
    /// Owns the level loop: what is on the board, what the preview is advertising, and when one
    /// becomes the other.
    ///
    /// Nothing else starts a level. The board and the fruit objectives used to build themselves in
    /// their own constructors, which was fine while there was one of each - it stopped being fine
    /// the moment a level could say how big the board is.
    ///
    /// The finished board stays up while the preview is on top of it, and the next level is only
    /// built when the player asks for it. Rebuilding during the pause before the panel arrives
    /// would show the board being wiped through the gap.
    /// </summary>
    [UsedImplicitly]
    public class LevelController : AbstractController
    {
        /// <summary>
        /// One fruit is not a board, it is a single connected blob. Two is a deliberate and very
        /// easy level; below two there is nothing to match against.
        /// </summary>
        private const int MinFruitsPerLevel = 2;

        /// <summary>
        /// Levels between interstitials. Ads are the only revenue and the gap between levels is the
        /// only break in play that is not the player's own idea, but one every level would be
        /// intolerable - and the platform rate-limits them anyway, so asking more often mostly
        /// gets refused.
        /// </summary>
        private const int LevelsPerInterstitial = 5;

        private readonly LevelModel _model;
        private readonly BoardController _boardController;
        private readonly FruitGoalController _fruitGoalController;
        private readonly ScoreController _scoreController;
        private readonly ScoreModel _scoreModel;
        private readonly GoalController _goalController;
        private readonly GoalModel _goalModel;
        private readonly SaveModel _saveModel;
        private readonly TileConfig _tileConfig;
        private readonly StarfruitPlacer _starfruitPlacer;
        private readonly IPlatformServices _platformServices;

        private readonly IReadOnlyList<LevelDefinition> _levels = LevelSet.Levels;

        /// Levels finished this session, which is what the interstitial cadence counts.
        private int _completedLevelCount;

        /// True once the board's level is finished and the preview has moved on to the next one.
        private bool _isNextLevelPending;

        /// <summary>
        /// Set by the completion that earned it, spent by the preview it appears on. A flag rather
        /// than a check at the point of showing, because the preview also opens the game and an ad
        /// on launch is exactly what nobody wants.
        /// </summary>
        private bool _isInterstitialDue;

        public LevelController(LevelModel model, BoardController boardController,
            FruitGoalController fruitGoalController, ScoreController scoreController,
            ScoreModel scoreModel, GoalController goalController, GoalModel goalModel,
            SaveModel saveModel, TileConfig tileConfig, StarfruitPlacer starfruitPlacer,
            IPlatformServices platformServices)
        {
            _model = model;
            _boardController = boardController;
            _fruitGoalController = fruitGoalController;
            _scoreController = scoreController;
            _scoreModel = scoreModel;
            _goalController = goalController;
            _goalModel = goalModel;
            _saveModel = saveModel;
            _tileConfig = tileConfig;
            _starfruitPlacer = starfruitPlacer;
            _platformServices = platformServices;

            if (_levels.Count == 0)
            {
                Debug.LogError("[LevelController] LevelSet is empty. There is no game to start.");
                return;
            }

            // The first level is built straight away and the preview goes up over it, so the player
            // opens onto a board rather than onto an empty frame.
            StartLevel(0);
            _model.SetPreviewLevel(0, _levels[0]);
            _goalController.ShowLevelPreview();

            _saveModel.IsLoaded.WhereTrue().First().SubscribeBlind(ApplySavedProgress).AddTo(Disposer);

            _goalModel.IsLevelComplete.WhereTrue().SubscribeBlind(CompleteLevel).AddTo(Disposer);

            // On the preview appearing rather than on the level ending, so the ad lands in the gap
            // between levels instead of over the moment the last fruit drops.
            _goalModel.IsLevelPreviewVisible.WhereTrue()
                .SubscribeBlind(ShowInterstitialIfDue)
                .AddTo(Disposer);
        }

        /// <summary>
        /// Starts whatever the preview is showing and takes it down. This is the preview panel's
        /// only button, on launch and between levels alike.
        /// </summary>
        public void StartPendingLevel()
        {
            if (_isNextLevelPending)
            {
                StartLevel(_model.PreviewLevelIndex.Value);
                _isNextLevelPending = false;
            }

            _goalController.HideLevelPreview();
        }

        private void CompleteLevel()
        {
            // Banked here rather than when the next level starts, so a player who closes the game
            // on the preview screen keeps what they just earned.
            _scoreModel.AddToGrandTotal(_scoreModel.Score.Value);

            // The last level is the end of the game, and saying so is a certification requirement
            // rather than a flourish. The preview still wraps to the first level afterwards, so
            // there is something to do - but it is a replay, and the panel says as much.
            if (_model.CurrentLevelIndex.Value >= _levels.Count - 1)
            {
                _saveModel.SetHasCompletedAllLevels(true);
                _model.SetIsContentComplete(true);
            }

            var nextLevelIndex = (_model.CurrentLevelIndex.Value + 1) % _levels.Count;

            _saveModel.SetCurrentLevelIndex(nextLevelIndex);

            _isNextLevelPending = true;

            // Counted on levels finished rather than on the index, so it stays on cadence through
            // the wrap back to level one.
            _completedLevelCount++;

            if (_completedLevelCount % LevelsPerInterstitial == 0)
            {
                _isInterstitialDue = true;
            }

            _model.SetPreviewLevel(nextLevelIndex, _levels[nextLevelIndex]);
        }

        private void ShowInterstitialIfDue()
        {
            if (!_isInterstitialDue)
            {
                return;
            }

            // Cleared before the request, not after. There is no completion callback to hang this
            // on, and a refused ad must not leave the flag set to fire on the next preview too.
            _isInterstitialDue = false;

            _platformServices.ShowInterstitialAd();
        }

        private void ApplySavedProgress()
        {
            _scoreModel.SetGrandTotal(_saveModel.Data.GrandTotalScore);

            if (_saveModel.Data.HasCompletedAllLevels)
            {
                _model.SetIsContentComplete(true);
            }

            // Only while the player has not started. A slow save arriving mid-level and yanking the
            // board out from under them is worse than losing one level of progress.
            if (!_goalModel.IsLevelPreviewVisible.Value)
            {
                return;
            }

            var savedLevelIndex = Mathf.Clamp(_saveModel.Data.CurrentLevelIndex, 0, _levels.Count - 1);

            if (savedLevelIndex == _model.CurrentLevelIndex.Value)
            {
                return;
            }

            StartLevel(savedLevelIndex);
            _model.SetPreviewLevel(savedLevelIndex, _levels[savedLevelIndex]);
        }

        private void StartLevel(int levelIndex)
        {
            var level = _levels[levelIndex];

            // Fruit first: the board spawns tiles the moment it is built, and it reads the set off
            // the model.
            _model.SetSpawnableTileSettings(ResolveFruits(level, levelIndex));
            _model.SetCurrentLevelIndex(levelIndex);

            _scoreController.RestartScore();

            _fruitGoalController.StartObjectives(level.Objectives);
            _boardController.StartBoard(level.BoardResolution);

            // After the board, obviously - they replace tiles that have to exist first.
            _starfruitPlacer.PlaceStartingStarfruits(level.StartingStarfruits);
        }

        /// <summary>
        /// Turns a level's fruit list into the tile settings the board spawns from. A level that
        /// names none gets the full pool, which is what makes Fruits an optional dial rather than
        /// something every level has to restate.
        /// </summary>
        private IReadOnlyList<TileSetting> ResolveFruits(LevelDefinition level, int levelIndex)
        {
            if (level.Fruits == null || level.Fruits.Count == 0)
            {
                return _tileConfig.SpawnableTileSettings;
            }

            var tileSettings = new List<TileSetting>();

            foreach (var fruit in level.Fruits)
            {
                // Every setting, not just the spawnable ones: a level may deliberately put Starfruit
                // in its pool, and the wildcard is excluded from the default pool precisely so that
                // has to be asked for.
                var tileSetting = _tileConfig.TileSettings
                    .FirstOrDefault(setting => setting.TileType == fruit);

                if (tileSetting == null)
                {
                    Debug.LogError($"[LevelController] Level {levelIndex + 1} asks for {fruit}, which " +
                                   "is not a spawnable tile in TileConfig. Dropping it.");
                    continue;
                }

                tileSettings.Add(tileSetting);
            }

            if (tileSettings.Count == 0)
            {
                Debug.LogError($"[LevelController] Level {levelIndex + 1} named no usable fruit. " +
                               "Falling back to the full spawn pool.");

                return _tileConfig.SpawnableTileSettings;
            }

            // Distinct, because repeats in the list are weighting rather than variety - a pool of
            // six apples and one lemon is a one-fruit board however long the list is.
            var distinctFruitCount = tileSettings.Select(setting => setting.TileType).Distinct().Count();

            if (distinctFruitCount < MinFruitsPerLevel)
            {
                Debug.LogError($"[LevelController] Level {levelIndex + 1} has {distinctFruitCount} " +
                               $"distinct fruit. Below {MinFruitsPerLevel} there is nothing to match " +
                               "against and the board stops being a puzzle.");
            }

            return tileSettings;
        }
    }
}
