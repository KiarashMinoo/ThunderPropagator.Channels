using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Playlist;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Playlist
{
    /// <summary>Issue #228's own AC: unknown or disabled ids are rejected, and only a registered id ever resolves to a real source.</summary>
    public sealed class InMemoryVideoPlaylistTests
    {
        // A real, policy-compliant root — #233's own scope made VideoPlaylistPolicy required, so every
        // entry these tests construct needs a Location that actually resolves inside some configured
        // root; the temp directory is a convenient, always-valid choice that has nothing to do with what
        // any of these tests are actually about (playlist lookup/duplicate/enable behavior, not policy
        // validation itself — see VideoPlaylistEntryValidatorTests for that).
        private static readonly string TestRoot = Path.Combine(Path.GetTempPath(), "video-playlist-tests");

        private static readonly VideoPlaylistPolicy PermissivePolicy = new() { LocalFileRoot = TestRoot };

        private static VideoPlaylistEntry MakeEntry(string videoId, bool isEnabled = true) => new()
        {
            VideoId = videoId,
            Title = $"Title for {videoId}",
            Source = new VideoSource { Location = Path.Combine(TestRoot, $"{videoId}.mp4") },
            IsEnabled = isEnabled
        };

        [Fact]
        public void TryGetEntry_KnownEnabledId_ReturnsTrueAndTheEntry()
        {
            var entry = MakeEntry("video-1");
            var playlist = new InMemoryVideoPlaylist([entry], PermissivePolicy);

            var found = playlist.TryGetEntry("video-1", out var resolved);

            Assert.True(found);
            Assert.Same(entry, resolved);
        }

        [Fact]
        public void TryGetEntry_UnknownId_ReturnsFalse()
        {
            var playlist = new InMemoryVideoPlaylist([MakeEntry("video-1")], PermissivePolicy);

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
            var playlist = new InMemoryVideoPlaylist([disabled], PermissivePolicy);

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
            var playlist = new InMemoryVideoPlaylist([MakeEntry("video-1")], PermissivePolicy);

            var found = playlist.TryGetEntry(videoId!, out var resolved);

            Assert.False(found);
            Assert.Null(resolved);
        }

        [Fact]
        public void Constructor_DuplicateVideoId_Throws()
        {
            var entries = new[] { MakeEntry("video-1"), MakeEntry("video-1") };

            Assert.Throws<ArgumentException>(() => new InMemoryVideoPlaylist(entries, PermissivePolicy));
        }

        [Fact]
        public void Constructor_DistinctVideoIds_AllResolvable()
        {
            var playlist = new InMemoryVideoPlaylist([MakeEntry("video-1"), MakeEntry("video-2")], PermissivePolicy);

            Assert.True(playlist.TryGetEntry("video-1", out _));
            Assert.True(playlist.TryGetEntry("video-2", out _));
        }

        [Fact]
        public void Constructor_EntryViolatingPolicy_ThrowsValidationException()
        {
            // #233's own scope: policy validation runs for every entry at construction, not just
            // duplicate-id checking — see VideoPlaylistEntryValidatorTests for the exhaustive policy
            // cases themselves.
            var outsideRoot = new VideoPlaylistEntry
            {
                VideoId = "video-1",
                Title = "Title",
                Source = new VideoSource { Location = "../../../etc/passwd" },
                IsEnabled = true
            };

            Assert.Throws<VideoPlaylistValidationException>(() => new InMemoryVideoPlaylist([outsideRoot], PermissivePolicy));
        }
    }
}
