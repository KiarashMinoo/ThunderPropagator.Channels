using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Playlist
{
    /// <summary>
    /// One server-approved video, keyed by its own client-safe <see cref="VideoId"/> — #228's own scope,
    /// "accept only IDs from the configured server playlist." <see cref="Source"/>'s own server-side
    /// location must never be forwarded to a client; only <see cref="VideoId"/>/<see cref="Title"/> ever
    /// reach one, exactly as <see cref="Media.VideoSource"/>'s own remarks already require.
    /// </summary>
    public sealed record VideoPlaylistEntry
    {
        /// <summary>The client-safe identifier a caller supplies to <c>Video/Select</c> — never a path or URL.</summary>
        public required string VideoId { get; init; }

        /// <summary>Human-readable title. Safe to display; never derived from <see cref="Source"/>'s own location.</summary>
        public required string Title { get; init; }

        /// <summary>Where the approved video actually lives — server-side only, resolved by <see cref="VideoId"/>, never exposed to a client.</summary>
        public required VideoSource Source { get; init; }

        /// <summary>
        /// Whether this entry can currently be selected. A known-but-disabled entry is rejected the same
        /// way as an unknown one — #228's own AC, "Unknown or disabled IDs are rejected" — so a client
        /// can never distinguish "this id doesn't exist" from "this id exists but is turned off."
        /// </summary>
        public bool IsEnabled { get; init; } = true;
    }
}
