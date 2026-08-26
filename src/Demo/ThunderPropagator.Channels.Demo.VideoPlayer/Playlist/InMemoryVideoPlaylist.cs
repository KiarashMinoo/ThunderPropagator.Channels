namespace ThunderPropagator.Channels.Demo.VideoPlayer.Playlist
{
    /// <summary>
    /// A static, in-memory <see cref="IVideoPlaylist"/> built once from whatever entries the caller
    /// supplies — deliberately the simplest possible implementation of the contract (no add/remove,
    /// no persistence, no runtime reconfiguration) pending #233's own fuller server-side playlist
    /// management. How those entries actually get here (configuration binding, a hardcoded list, etc.)
    /// and how this type gets registered in DI are both out of this ticket's own scope — #238's job,
    /// same as <c>VideoPlaybackSessionManager</c>'s own registration.
    /// </summary>
    public sealed class InMemoryVideoPlaylist : IVideoPlaylist
    {
        private readonly IReadOnlyDictionary<string, VideoPlaylistEntry> _entriesByVideoId;

        /// <exception cref="ArgumentException">
        /// Two entries share the same <see cref="VideoPlaylistEntry.VideoId"/> — for a security-relevant
        /// allow-list, silently letting one shadow the other (last-wins) would mask a configuration
        /// mistake far more dangerously than just failing loudly at construction time.
        /// </exception>
        public InMemoryVideoPlaylist(IEnumerable<VideoPlaylistEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            var byId = new Dictionary<string, VideoPlaylistEntry>();
            foreach (var entry in entries)
            {
                if (!byId.TryAdd(entry.VideoId, entry))
                    throw new ArgumentException($"Duplicate {nameof(VideoPlaylistEntry.VideoId)} '{entry.VideoId}'.", nameof(entries));
            }

            _entriesByVideoId = byId;
        }

        /// <inheritdoc/>
        public bool TryGetEntry(string videoId, out VideoPlaylistEntry? entry)
        {
            if (string.IsNullOrWhiteSpace(videoId))
            {
                entry = null;
                return false;
            }

            return _entriesByVideoId.TryGetValue(videoId, out entry);
        }
    }
}
