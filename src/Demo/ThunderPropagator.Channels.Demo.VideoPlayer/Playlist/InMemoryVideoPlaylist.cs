namespace ThunderPropagator.Channels.Demo.VideoPlayer.Playlist
{
    /// <summary>
    /// A static, in-memory <see cref="IVideoPlaylist"/> built once from whatever entries the caller
    /// supplies — deliberately the simplest possible implementation of the contract (no add/remove,
    /// no persistence, no runtime reconfiguration) pending any future fuller server-side playlist
    /// management. <c>AddVideoPlayerChannel</c> (#238) constructs and registers the one instance a
    /// deployment actually uses, from <see cref="Configuration.VideoPlayerChannelConfiguration.PlaylistEntries"/>/
    /// <see cref="Configuration.VideoPlayerChannelConfiguration.PlaylistPolicy"/> — this type itself has
    /// no opinion on where those values come from.
    /// </summary>
    public sealed class InMemoryVideoPlaylist : IVideoPlaylist
    {
        private readonly IReadOnlyDictionary<string, VideoPlaylistEntry> _entriesByVideoId;

        /// <summary>
        /// Validates every entry against <paramref name="policy"/> (see
        /// <see cref="VideoPlaylistEntryValidator"/>) and checks for duplicate
        /// <see cref="VideoPlaylistEntry.VideoId"/>s, all before this constructor returns — #233's own
        /// scope, "validate... at startup": a playlist that fails validation fails to construct at all
        /// rather than lazily failing on first use. <paramref name="policy"/> is required, not optional —
        /// #233's own scope, "define local-file root restrictions and remote-fetch protections" — there
        /// is deliberately no way to construct a playlist that silently skips policy validation.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Two entries share the same <see cref="VideoPlaylistEntry.VideoId"/> — for a security-relevant
        /// allow-list, silently letting one shadow the other (last-wins) would mask a configuration
        /// mistake far more dangerously than just failing loudly at construction time.
        /// </exception>
        /// <exception cref="VideoPlaylistValidationException">An entry's own source does not satisfy <paramref name="policy"/>.</exception>
        public InMemoryVideoPlaylist(IEnumerable<VideoPlaylistEntry> entries, VideoPlaylistPolicy policy)
        {
            ArgumentNullException.ThrowIfNull(entries);
            ArgumentNullException.ThrowIfNull(policy);

            var byId = new Dictionary<string, VideoPlaylistEntry>();
            foreach (var entry in entries)
            {
                if (!byId.TryAdd(entry.VideoId, entry))
                    throw new ArgumentException($"Duplicate {nameof(VideoPlaylistEntry.VideoId)} '{entry.VideoId}'.", nameof(entries));

                VideoPlaylistEntryValidator.Validate(entry, policy);
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
