namespace Platform
{
    public enum AdResult
    {
        /// Ad played to completion and the reward should be granted.
        Granted,

        /// No ad was available to serve. Expected in normal operation - fill is well below 100%
        /// and ads only serve in some geos, so this must never block play.
        NoFill,

        /// An ad was served but the player closed it before earning the reward.
        Dismissed,

        /// The request failed. Treated the same as NoFill by callers.
        Error
    }
}
