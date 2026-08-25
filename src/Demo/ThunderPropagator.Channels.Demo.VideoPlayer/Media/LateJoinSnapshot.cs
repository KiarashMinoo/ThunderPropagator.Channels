namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// What <see cref="VideoPlaybackSession"/>'s own <c>Join</c> method hands back — enough for a caller to populate a
    /// client-facing state message (mirroring <see cref="VideoPlayerChannelFeederMessage"/>'s own
    /// <c>Epoch</c>/<c>CurrentFrameNumber</c>/<c>MediaPosition</c>/<c>SyncTime</c>/<c>State</c> fields)
    /// without re-deriving them from a second, separately-timed read of session state — #223's own AC,
    /// "Capture one atomic snapshot containing epoch, position, frame number, state, and synchronization
    /// time."
    /// </summary>
    /// <remarks>
    /// Every field here is derived from the exact same bootstrap frame that call captured (or, if none
    /// has been published yet in the current epoch, from the session's own current state/epoch) — never
    /// from two separately-timed reads of session state, which is what would let a snapshot and its own
    /// bootstrap frame silently drift into different epochs.
    /// </remarks>
    public sealed record LateJoinSnapshot
    {
        /// <summary>The session's lifecycle state as of this snapshot.</summary>
        public required PlayState State { get; init; }

        /// <summary>The epoch <see cref="HasBootstrapFrame"/>'s frame belongs to, or the session's current epoch if none has been published yet.</summary>
        public required int Epoch { get; init; }

        /// <summary>Whether a bootstrap frame existed to unicast — when <see langword="false"/>, every other field below is a default/zero value, matching a freshly-selected, not-yet-playing session.</summary>
        public required bool HasBootstrapFrame { get; init; }

        /// <summary>0-based number of the bootstrap frame within <see cref="Epoch"/>. Zero if <see cref="HasBootstrapFrame"/> is <see langword="false"/>.</summary>
        public required long FrameNumber { get; init; }

        /// <summary>The bootstrap frame's own presentation timestamp — the playback position this snapshot represents.</summary>
        public required TimeSpan MediaPosition { get; init; }

        /// <summary>The bootstrap frame's own <see cref="VideoFramePacket.DisplayTime"/> — the monotonic-clock reading <see cref="MediaPosition"/> was measured at, letting a joiner extrapolate the expected live position while <see cref="State"/> is <see cref="PlayState.Playing"/>.</summary>
        public required TimeSpan SyncTime { get; init; }
    }
}
