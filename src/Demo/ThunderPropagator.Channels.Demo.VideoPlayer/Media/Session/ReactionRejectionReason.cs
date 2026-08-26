namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session
{
    /// <summary>Why <see cref="ReactionAggregator.TryRecord"/> refused one reaction — #229's own scope.</summary>
    public enum ReactionRejectionReason
    {
        /// <summary>The reaction string is not currently in <see cref="ReactionAggregator"/>'s own allowed set — either never a real reaction type, or one that has since been disabled by removing it from that set. Both read the same way to a caller: this id is not currently selectable.</summary>
        Unknown,

        /// <summary>The reaction string exceeds <c>VideoPlayerChannelFeederMessage.ReactionNameMaxLength</c>.</summary>
        TooLong,

        /// <summary>The calling viewer has already recorded the configured maximum number of reactions within the trailing reaction window.</summary>
        RateLimited
    }
}
