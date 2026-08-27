namespace ThunderPropagator.Channels.Demo.VideoPlayer.Playlist
{
    /// <summary>
    /// Thrown by <see cref="VideoPlaylistEntryValidator"/> (see #233) when a playlist entry's own
    /// <see cref="VideoPlaylistEntry.Source"/> does not satisfy a <see cref="VideoPlaylistPolicy"/> — a
    /// startup/configuration-time failure, not a wire-facing one (unlike <c>Video/Select</c>'s own
    /// exceptions, this never reaches a client and so carries no <see cref="System.Net.HttpStatusCode"/>).
    /// The message always describes <i>which rule</i> was violated, never the rejected location itself —
    /// echoing the raw value back would itself leak exactly what this validation exists to protect.
    /// </summary>
    public sealed class VideoPlaylistValidationException(string message) : Exception(message)
    {
    }
}
