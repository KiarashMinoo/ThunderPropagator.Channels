using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Playlist;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Playlist
{
    /// <summary>Issue #228's own AC: unknown or disabled ids are rejected, and only a registered id ever resolves to a real source.</summary>
    public sealed class InMemoryVideoPlaylistTests
    {
        private static VideoPlaylistEntry MakeEntry(string videoId, bool isEnabled = true) => new()
        {
            VideoId = videoId,
            Title = $"Title for {videoId}",
            Source = new VideoSource { Location = $"server-only-location-for-{videoId}" },
            IsEnabled = isEnabled
        };

        [Fact]
        public void TryGetEntry_KnownEnabledId_ReturnsTrueAndTheEntry()
        {
            var entry = MakeEntry("video-1");
            var playlist = new InMemoryVideoPlaylist([entry]);

            var found = playlist.TryGetEntry("video-1", out var resolved);

            Assert.True(found);
            Assert.Same(entry, resolved);
        }

        [Fact]
        public void TryGetEntry_UnknownId_ReturnsFalse()
        {
            var playlist = new InMemoryVideoPlaylist([MakeEntry("video-1")]);

            var found = playlist.TryGetEntry("does-not-exist", out var resolved);

            Assert.False(found);
            Assert.Null(resolved);
        }

        [Fact]
        public void TryGetEntry_KnownButDisabledId_StillReturnsTrueAndTheEntry()
        {
            // IVideoPlaylist itself does not enforce IsEnabled — see its own remarks on why that
            // decision belongs to the caller (e.g. Video/Select), not this raw-lookup contract.
            var disabled = MakeEntry("video-1", isEnabled: false);
            var playlist = new InMemoryVideoPlaylist([disabled]);

            var found = playlist.TryGetEntry("video-1", out var resolved);

            Assert.True(found);
            Assert.NotNull(resolved);
            Assert.False(resolved!.IsEnabled);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TryGetEntry_NullOrWhitespaceId_ReturnsFalse(string? videoId)
        {
            var playlist = new InMemoryVideoPlaylist([MakeEntry("video-1")]);

            var found = playlist.TryGetEntry(videoId!, out var resolved);

            Assert.False(found);
            Assert.Null(resolved);
        }

        [Fact]
        public void Constructor_DuplicateVideoId_Throws()
        {
            var entries = new[] { MakeEntry("video-1"), MakeEntry("video-1") };

            Assert.Throws<ArgumentException>(() => new InMemoryVideoPlaylist(entries));
        }

        [Fact]
        public void Constructor_DistinctVideoIds_AllResolvable()
        {
            var playlist = new InMemoryVideoPlaylist([MakeEntry("video-1"), MakeEntry("video-2")]);

            Assert.True(playlist.TryGetEntry("video-1", out _));
            Assert.True(playlist.TryGetEntry("video-2", out _));
        }
    }
}
