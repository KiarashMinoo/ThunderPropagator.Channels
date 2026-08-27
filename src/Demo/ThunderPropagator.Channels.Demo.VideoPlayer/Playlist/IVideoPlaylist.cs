namespace ThunderPropagator.Channels.Demo.VideoPlayer.Playlist
{
    /// <summary>
    /// The server's approved video allow-list — #228's own scope, resolving a client-safe
    /// <see cref="VideoPlaylistEntry.VideoId"/> to its actual <see cref="Media.VideoSource"/> so no
    /// client-supplied path or URL can ever reach a decoder. This contract is deliberately minimal (a
    /// raw lookup only, no add/remove/persistence) — #233 built policy-based startup validation
    /// (<see cref="VideoPlaylistEntryValidator"/>/<see cref="VideoPlaylistPolicy"/>) on top of this
    /// contract via <see cref="InMemoryVideoPlaylist"/> rather than replacing it; any future fuller
    /// management system (persistence, admin API, dynamic reconfiguration) can do the same.
    /// </summary>
    public interface IVideoPlaylist
    {
        /// <summary>
        /// Looks up <paramref name="videoId"/> without regard to <see cref="VideoPlaylistEntry.IsEnabled"/>
        /// — a caller (e.g. <c>Video/Select</c>) decides how to react to a disabled entry itself, rather
        /// than this contract baking in one specific rejection policy.
        /// </summary>
        /// <returns><see langword="true"/> if an entry is registered under <paramref name="videoId"/>, regardless of whether it is enabled.</returns>
        bool TryGetEntry(string videoId, out VideoPlaylistEntry? entry);
    }
}
