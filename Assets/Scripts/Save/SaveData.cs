using System;

namespace Save
{
    /// <summary>
    /// The cloud save payload. Serialized with JsonUtility, so fields must be public and the type
    /// must stay [Serializable].
    ///
    /// Rules that outlive any one build:
    /// - Only ever ADD fields. Renaming or removing one silently drops a returning player's data.
    /// - Bump <see cref="CurrentSchemaVersion"/> and add a migration whenever the meaning of an
    ///   existing field changes.
    /// - New fields must be meaningful at their default value, because every save written before
    ///   they existed will deserialize with exactly that.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>
        /// Version of the schema this payload was written with. Version-stamped from the first
        /// shipped build, because retrofitting a version onto saves already in the wild is
        /// guesswork.
        /// </summary>
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;

        /// The best score ever achieved. Also what gets sent to the platform leaderboard, so the
        /// two cannot disagree.
        public float HighScore;

        /// <summary>
        /// Every level's score added together, and the value sent to the platform leaderboard. It
        /// only ever grows, so the certification rule that the score sent matches the score saved
        /// holds by construction rather than by care.
        ///
        /// Added after v1 shipped and deliberately not a schema bump - zero is the right reading of
        /// a save written before levels existed.
        /// </summary>
        public float GrandTotalScore;

        /// <summary>
        /// Zero based index of the level to resume on. Zero is correct for an older save: a player
        /// from before levels existed starts at the first one.
        /// </summary>
        public int CurrentLevelIndex;

        /// <summary>
        /// Whether the last level has ever been finished. Certification requires the game to say
        /// plainly when there is no more content, and this is what makes that claim survive a
        /// restart rather than flashing past once.
        ///
        /// False is right for any older save - nobody finished a hundred levels before there were
        /// a hundred levels.
        /// </summary>
        public bool HasCompletedAllLevels;

        /// <summary>
        /// Whether the first-launch tutorial has been dismissed. Added after v1 shipped, and
        /// deliberately not a schema bump: adding a field needs no migration, and false is the
        /// correct reading of an older save - someone who saved before this existed has not seen it.
        /// </summary>
        public bool HasSeenTutorial;
    }
}
