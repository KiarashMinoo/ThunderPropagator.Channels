namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Describes where an <see cref="IVideoFrameSource"/> should read media from — a server-side-only
    /// concern. #216's own AC: "Keep server-side source locations out of public client DTOs."
    /// <see cref="Location"/> must never be forwarded into <see cref="VideoPlayerChannelFeederMessage"/>,
    /// any receive-pipeline response, or any other client-facing contract — only the approved playlist
    /// id a future ticket (#233) resolves to it should ever be client-visible.
    /// </summary>
    public sealed record VideoSource
    {
        /// <summary>The file path, URI, or decoder-specific connection string identifying the media. Server-side use only — see this type's own remarks.</summary>
        public required string Location { get; init; }

        /// <summary>Optional decoder-specific hints (e.g. a specific track index, a hardware-acceleration preference). Interpretation is entirely up to the concrete <see cref="IVideoFrameSource"/>.</summary>
        public IReadOnlyDictionary<string, string> Options { get; init; } = new Dictionary<string, string>();
    }
}
