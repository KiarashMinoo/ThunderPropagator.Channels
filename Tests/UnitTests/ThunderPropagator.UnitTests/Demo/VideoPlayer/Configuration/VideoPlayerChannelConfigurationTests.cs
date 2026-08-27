using ThunderPropagator.Channels.Demo.VideoPlayer.Configuration;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;
using ThunderPropagator.Channels.Demo.VideoPlayer.Playlist;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Configuration
{
    /// <summary>Issue #234's own AC: valid, boundary, and invalid configuration combinations.</summary>
    public sealed class VideoPlayerChannelConfigurationTests
    {
        // Same convenient, always-valid root #233's own InMemoryVideoPlaylistTests uses — see that
        // file's own remarks. Only relevant to the DefaultVideoId-cross-validation tests below.
        private static readonly string TestRoot = Path.Combine(Path.GetTempPath(), "video-player-configuration-tests");
        private static readonly VideoPlaylistPolicy PermissivePolicy = new() { LocalFileRoot = TestRoot };

        private static VideoPlaylistEntry MakeEntry(string videoId, bool isEnabled = true) => new()
        {
            VideoId = videoId,
            Title = $"Title for {videoId}",
            Source = new VideoSource { Location = Path.Combine(TestRoot, $"{videoId}.mp4") },
            IsEnabled = isEnabled
        };

        [Fact]
        public void Validate_DefaultConfiguration_DoesNotThrow()
        {
            var configuration = new VideoPlayerChannelConfiguration();

            var exception = Record.Exception(() => configuration.Validate());

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(VideoPlayerChannelConfiguration.MinDimension)]
        [InlineData(VideoPlayerChannelConfiguration.MaxDimensionWidth)]
        public void Validate_MaxWidth_AtBoundary_DoesNotThrow(int width)
        {
            var configuration = new VideoPlayerChannelConfiguration { MaxWidth = width };

            var exception = Record.Exception(() => configuration.Validate());

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(VideoPlayerChannelConfiguration.MinDimension - 1)]
        [InlineData(VideoPlayerChannelConfiguration.MaxDimensionWidth + 1)]
        public void Validate_MaxWidth_OutsideBoundary_ThrowsNamingMaxWidth(int width)
        {
            var configuration = new VideoPlayerChannelConfiguration { MaxWidth = width };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.MaxWidth), exception.ParamName);
        }

        [Theory]
        [InlineData(VideoPlayerChannelConfiguration.MinDimension)]
        [InlineData(VideoPlayerChannelConfiguration.MaxDimensionHeight)]
        public void Validate_MaxHeight_AtBoundary_DoesNotThrow(int height)
        {
            var configuration = new VideoPlayerChannelConfiguration { MaxHeight = height };

            var exception = Record.Exception(() => configuration.Validate());

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(VideoPlayerChannelConfiguration.MinDimension - 1)]
        [InlineData(VideoPlayerChannelConfiguration.MaxDimensionHeight + 1)]
        public void Validate_MaxHeight_OutsideBoundary_ThrowsNamingMaxHeight(int height)
        {
            var configuration = new VideoPlayerChannelConfiguration { MaxHeight = height };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.MaxHeight), exception.ParamName);
        }

        [Theory]
        [InlineData(VideoFrameEncoder.MinQuality)]
        [InlineData(VideoFrameEncoder.MaxQuality)]
        public void Validate_Quality_AtBoundary_DoesNotThrow(int quality)
        {
            var configuration = new VideoPlayerChannelConfiguration { Quality = quality };

            var exception = Record.Exception(() => configuration.Validate());

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(VideoFrameEncoder.MinQuality - 1)]
        [InlineData(VideoFrameEncoder.MaxQuality + 1)]
        public void Validate_Quality_OutsideBoundary_ThrowsNamingQuality(int quality)
        {
            var configuration = new VideoPlayerChannelConfiguration { Quality = quality };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.Quality), exception.ParamName);
        }

        [Fact]
        public void Validate_UndefinedEncoding_ThrowsNamingEncoding()
        {
            var configuration = new VideoPlayerChannelConfiguration { Encoding = (VideoFramePacketEncoding)255 };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.Encoding), exception.ParamName);
        }

        [Fact]
        public void Validate_UndefinedAudioEncoding_ThrowsNamingAudioEncoding()
        {
            var configuration = new VideoPlayerChannelConfiguration { AudioEncoding = (AudioFramePacketEncoding)255 };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.AudioEncoding), exception.ParamName);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(VideoPlayerChannelConfiguration.MaxAudioBitRate)]
        public void Validate_AudioBitRate_AtBoundary_DoesNotThrow(int bitRate)
        {
            var configuration = new VideoPlayerChannelConfiguration { AudioBitRate = bitRate };

            var exception = Record.Exception(() => configuration.Validate());

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(VideoPlayerChannelConfiguration.MaxAudioBitRate + 1)]
        public void Validate_AudioBitRate_OutsideBoundary_ThrowsNamingAudioBitRate(int bitRate)
        {
            var configuration = new VideoPlayerChannelConfiguration { AudioBitRate = bitRate };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.AudioBitRate), exception.ParamName);
        }

        [Fact]
        public void Validate_EnableAudioFalse_WithUnusualAudioSettingsStillAtDefaults_DoesNotThrow()
        {
            // Proves the cross-field tolerance documented on EnableAudio/the audio-only settings below
            // it: they are still validated (see the boundary tests above), but disabling audio itself is
            // never blocked by anything about them being merely "unusual" — only genuinely out-of-range.
            var configuration = new VideoPlayerChannelConfiguration { EnableAudio = false };

            var exception = Record.Exception(() => configuration.Validate());

            Assert.Null(exception);
        }

        [Fact]
        public void Validate_EnableReactionsFalse_WithDefaultReactionSettings_DoesNotThrow()
        {
            var configuration = new VideoPlayerChannelConfiguration { EnableReactions = false };

            var exception = Record.Exception(() => configuration.Validate());

            Assert.Null(exception);
        }

        [Fact]
        public void Validate_ZeroReactionWindow_ThrowsNamingReactionWindow()
        {
            var configuration = new VideoPlayerChannelConfiguration { ReactionWindow = TimeSpan.Zero };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.ReactionWindow), exception.ParamName);
        }

        [Fact]
        public void Validate_ZeroMaxReactionsPerViewerPerWindow_ThrowsNamingMaxReactionsPerViewerPerWindow()
        {
            var configuration = new VideoPlayerChannelConfiguration { MaxReactionsPerViewerPerWindow = 0 };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.MaxReactionsPerViewerPerWindow), exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_ZeroOrNegativeDecodeBufferCapacity_ThrowsNamingDecodeBufferCapacity(int capacity)
        {
            var configuration = new VideoPlayerChannelConfiguration { DecodeBufferCapacity = capacity };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.DecodeBufferCapacity), exception.ParamName);
        }

        [Fact]
        public void Validate_ZeroSubscriberQueueCapacity_ThrowsNamingSubscriberQueueCapacity()
        {
            var configuration = new VideoPlayerChannelConfiguration { SubscriberQueueCapacity = 0 };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.SubscriberQueueCapacity), exception.ParamName);
        }

        [Fact]
        public void Validate_ZeroAudioDecodeBufferCapacity_ThrowsNamingAudioDecodeBufferCapacity()
        {
            var configuration = new VideoPlayerChannelConfiguration { AudioDecodeBufferCapacity = 0 };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.AudioDecodeBufferCapacity), exception.ParamName);
        }

        [Fact]
        public void Validate_ZeroAudioSubscriberQueueCapacity_ThrowsNamingAudioSubscriberQueueCapacity()
        {
            var configuration = new VideoPlayerChannelConfiguration { AudioSubscriberQueueCapacity = 0 };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.AudioSubscriberQueueCapacity), exception.ParamName);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        public void Validate_NonPositivePlaybackRate_ThrowsNamingPlaybackRate(double rate)
        {
            var configuration = new VideoPlayerChannelConfiguration { PlaybackRate = rate };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.PlaybackRate), exception.ParamName);
        }

        [Fact]
        public void Validate_ZeroPollInterval_ThrowsNamingPollInterval()
        {
            var configuration = new VideoPlayerChannelConfiguration { PollInterval = TimeSpan.Zero };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.PollInterval), exception.ParamName);
        }

        [Fact]
        public void Validate_ZeroSourceOpenTimeout_ThrowsNamingSourceOpenTimeout()
        {
            var configuration = new VideoPlayerChannelConfiguration { SourceOpenTimeout = TimeSpan.Zero };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.SourceOpenTimeout), exception.ParamName);
        }

        [Fact]
        public void Validate_NullSourceOpenTimeout_DoesNotThrow()
        {
            var configuration = new VideoPlayerChannelConfiguration { SourceOpenTimeout = null };

            var exception = Record.Exception(() => configuration.Validate());

            Assert.Null(exception);
        }

        [Fact]
        public void Validate_ZeroMaxPublishLatenessBeforeBuffering_ThrowsNamingMaxPublishLatenessBeforeBuffering()
        {
            var configuration = new VideoPlayerChannelConfiguration { MaxPublishLatenessBeforeBuffering = TimeSpan.Zero };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.MaxPublishLatenessBeforeBuffering), exception.ParamName);
        }

        [Fact]
        public void Validate_ZeroIdleSessionRetention_ThrowsNamingIdleSessionRetention()
        {
            var configuration = new VideoPlayerChannelConfiguration { IdleSessionRetention = TimeSpan.Zero };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.IdleSessionRetention), exception.ParamName);
        }

        [Fact]
        public void Validate_NullIdleSessionRetention_DoesNotThrow()
        {
            var configuration = new VideoPlayerChannelConfiguration { IdleSessionRetention = null };

            var exception = Record.Exception(() => configuration.Validate());

            Assert.Null(exception);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WhitespaceOnlySessionId_ThrowsNamingSessionId(string sessionId)
        {
            var configuration = new VideoPlayerChannelConfiguration { SessionId = sessionId };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.SessionId), exception.ParamName);
        }

        [Fact]
        public void Validate_SessionIdTooLong_ThrowsNamingSessionId()
        {
            var configuration = new VideoPlayerChannelConfiguration { SessionId = new string('a', 129) };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.SessionId), exception.ParamName);
        }

        [Fact]
        public void Validate_NullSessionId_DoesNotThrow()
        {
            var configuration = new VideoPlayerChannelConfiguration { SessionId = null };

            var exception = Record.Exception(() => configuration.Validate());

            Assert.Null(exception);
        }

        [Fact]
        public void Validate_DefaultVideoIdSet_NoPlaylistSupplied_DoesNotThrow()
        {
            // The graceful-degradation path: nothing constructs an IVideoPlaylist in DI yet (#238's own
            // unfulfilled scope), so this specific cross-check is skipped, not failed, when no playlist
            // is supplied — every other validation in this method still runs regardless.
            var configuration = new VideoPlayerChannelConfiguration { DefaultVideoId = "does-not-exist-anywhere" };

            var exception = Record.Exception(() => configuration.Validate());

            Assert.Null(exception);
        }

        [Fact]
        public void Validate_DefaultVideoId_KnownAndEnabled_WithPlaylistSupplied_DoesNotThrow()
        {
            var playlist = new InMemoryVideoPlaylist([MakeEntry("video-1")], PermissivePolicy);
            var configuration = new VideoPlayerChannelConfiguration { DefaultVideoId = "video-1" };

            var exception = Record.Exception(() => configuration.Validate(playlist));

            Assert.Null(exception);
        }

        [Fact]
        public void Validate_DefaultVideoId_UnknownId_WithPlaylistSupplied_ThrowsNamingDefaultVideoId()
        {
            var playlist = new InMemoryVideoPlaylist([MakeEntry("video-1")], PermissivePolicy);
            var configuration = new VideoPlayerChannelConfiguration { DefaultVideoId = "does-not-exist" };

            var exception = Assert.Throws<ArgumentException>(() => configuration.Validate(playlist));

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.DefaultVideoId), exception.ParamName);
        }

        [Fact]
        public void Validate_DefaultVideoId_KnownButDisabled_WithPlaylistSupplied_ThrowsNamingDefaultVideoId()
        {
            var playlist = new InMemoryVideoPlaylist([MakeEntry("video-1", isEnabled: false)], PermissivePolicy);
            var configuration = new VideoPlayerChannelConfiguration { DefaultVideoId = "video-1" };

            var exception = Assert.Throws<ArgumentException>(() => configuration.Validate(playlist));

            Assert.Equal(nameof(VideoPlayerChannelConfiguration.DefaultVideoId), exception.ParamName);
        }
    }
}
