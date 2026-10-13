namespace Interaction
{
    /// <summary>
    /// Why board input is suppressed. Tracked as a set rather than a single flag because the
    /// reasons overlap - buying a booster on the game over screen would otherwise end with the ad
    /// clearing the block that the finished run had put there.
    /// </summary>
    public enum InputBlockReason
    {
        Ad,
        GameOver,

        /// YouTube told us to pause. Outranks nothing and is cleared only by onResume.
        PlatformPause,

        /// The first-launch tutorial is up. The overlay does not cover the whole screen, so the
        /// board has to be blocked explicitly rather than relying on the panel to swallow clicks.
        Tutorial
    }
}
