using ThunderPropagator.Channels.Demo.VideoPlayer;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer
{
    /// <summary>
    /// Issue #215's own ACs: fields round-trip (including microsecond-precision positions/epoch),
    /// per-property setters reject invalid values immediately, and
    /// <see cref="VideoPlayerChannelFeederMessage.ValidateForCurrentState"/> rejects a message whose
    /// fields are inconsistent with its own <see cref="PlayState"/>.
    /// </summary>
    public sealed class VideoPlayerChannelFeederMessageTests
    {
        private static VideoPlayerChannelFeederMessage CreateValid(PlayState state = PlayState.Playing) => new()
        {
            SessionId = "session-1",
            VideoId = "video-42",
            Title = "A Sample Video",
            State = state,
            Host = "Alice",
            SourceFrameRate = 29.97
        };

        [Fact]
        public void State_WhenNeverSet_DefaultsToLoading()
        {
            var message = new VideoPlayerChannelFeederMessage();

            Assert.Equal(PlayState.Loading, message.State);
        }

        [Fact]
        public void Reactions_WhenNeverSet_IsEmptyNotNull()
        {
            var message = new VideoPlayerChannelFeederMessage();

            Assert.Empty(message.Reactions);
        }

        [Fact]
        public void IdentityAndDisplayFields_RoundTrip()
        {
            var message = new VideoPlayerChannelFeederMessage
            {
                SessionId = "session-1",
                VideoId = "video-42",
                Title = "A Sample Video",
                Host = "Alice"
            };

            Assert.Equal("session-1", message.SessionId);
            Assert.Equal("video-42", message.VideoId);
            Assert.Equal("A Sample Video", message.Title);
            Assert.Equal("Alice", message.Host);
        }

        [Fact]
        public void EpochAndFrameAndTimingFields_RoundTripAtMicrosecondPrecision()
        {
            // Issue #215's own AC: "State serialization preserves microsecond positions and epoch
            // values." One tick short of a full second exercises sub-second precision explicitly.
            const long almostOneSecondInMicroseconds = 999_999;

            var message = new VideoPlayerChannelFeederMessage
            {
                Epoch = 7,
                CurrentFrameNumber = 123_456_789,
                MediaPosition = almostOneSecondInMicroseconds,
                SyncTime = almostOneSecondInMicroseconds + 42,
                Duration = 3_600_000_000L // one hour, in microseconds
            };

            Assert.Equal(7, message.Epoch);
            Assert.Equal(123_456_789, message.CurrentFrameNumber);
            Assert.Equal(almostOneSecondInMicroseconds, message.MediaPosition);
            Assert.Equal(almostOneSecondInMicroseconds + 42, message.SyncTime);
            Assert.Equal(3_600_000_000L, message.Duration);
        }

        [Fact]
        public void ViewerCountAndSourceFrameRate_RoundTrip()
        {
            var message = new VideoPlayerChannelFeederMessage
            {
                ViewerCount = 12,
                SourceFrameRate = 23.976
            };

            Assert.Equal(12, message.ViewerCount);
            Assert.Equal(23.976, message.SourceFrameRate);
        }

        [Fact]
        public void Reactions_RoundTrips()
        {
            VideoReactionCount[] reactions = [new("👍", 10), new("😂", 3)];

            var message = new VideoPlayerChannelFeederMessage { Reactions = reactions };

            Assert.Equal(reactions, message.Reactions);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void SessionId_WhenNullEmptyOrWhitespace_Throws(string? sessionId)
        {
            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { SessionId = sessionId! });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.SessionId), exception.PropertyName);
        }

        [Fact]
        public void SessionId_WhenOverMaxLength_Throws()
        {
            var tooLong = new string('a', VideoPlayerChannelFeederMessage.SessionIdMaxLength + 1);

            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { SessionId = tooLong });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.SessionId), exception.PropertyName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Host_WhenNullEmptyOrWhitespace_Throws(string? host)
        {
            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { Host = host! });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.Host), exception.PropertyName);
        }

        [Fact]
        public void VideoId_WhenOverMaxLength_Throws()
        {
            var tooLong = new string('a', VideoPlayerChannelFeederMessage.VideoIdMaxLength + 1);

            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { VideoId = tooLong });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.VideoId), exception.PropertyName);
        }

        [Fact]
        public void Title_WhenOverMaxLength_Throws()
        {
            var tooLong = new string('a', VideoPlayerChannelFeederMessage.TitleMaxLength + 1);

            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { Title = tooLong });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.Title), exception.PropertyName);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Epoch_WhenNegative_Throws(int value)
        {
            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { Epoch = value });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.Epoch), exception.PropertyName);
        }

        [Fact]
        public void CurrentFrameNumber_WhenNegative_Throws()
        {
            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { CurrentFrameNumber = -1 });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.CurrentFrameNumber), exception.PropertyName);
        }

        [Fact]
        public void MediaPosition_WhenNegative_Throws()
        {
            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { MediaPosition = -1 });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.MediaPosition), exception.PropertyName);
        }

        [Fact]
        public void SyncTime_WhenNegative_Throws()
        {
            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { SyncTime = -1 });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.SyncTime), exception.PropertyName);
        }

        [Fact]
        public void ViewerCount_WhenNegative_Throws()
        {
            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { ViewerCount = -1 });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.ViewerCount), exception.PropertyName);
        }

        [Fact]
        public void Duration_WhenNegative_Throws()
        {
            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { Duration = -1 });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.Duration), exception.PropertyName);
        }

        [Fact]
        public void SourceFrameRate_WhenNegative_Throws()
        {
            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { SourceFrameRate = -1 });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.SourceFrameRate), exception.PropertyName);
        }

        [Fact]
        public void Reactions_WhenOverMaxCount_Throws()
        {
            var tooMany = Enumerable.Range(0, VideoPlayerChannelFeederMessage.ReactionsMaxCount + 1)
                .Select(i => new VideoReactionCount($"r{i}", 1))
                .ToArray();

            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { Reactions = tooMany });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.Reactions), exception.PropertyName);
        }

        [Fact]
        public void Reactions_WithEmptyReactionName_Throws()
        {
            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { Reactions = [new VideoReactionCount("", 1)] });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.Reactions), exception.PropertyName);
        }

        [Fact]
        public void Reactions_WithNegativeCount_Throws()
        {
            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(
                () => new VideoPlayerChannelFeederMessage { Reactions = [new VideoReactionCount("👍", -1)] });

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.Reactions), exception.PropertyName);
        }

        [Theory]
        [InlineData(PlayState.Loading)]
        [InlineData(PlayState.Playing)]
        [InlineData(PlayState.Paused)]
        [InlineData(PlayState.Buffering)]
        [InlineData(PlayState.Ended)]
        public void ValidateForCurrentState_WithValidMessage_DoesNotThrow(PlayState state)
        {
            var message = CreateValid(state);

            var exception = Record.Exception(message.ValidateForCurrentState);

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(PlayState.Loading)]
        [InlineData(PlayState.Playing)]
        [InlineData(PlayState.Paused)]
        [InlineData(PlayState.Buffering)]
        [InlineData(PlayState.Ended)]
        public void ValidateForCurrentState_WithEmptyVideoId_ThrowsUnlessFaulted(PlayState state)
        {
            var message = CreateValid(state);
            message.VideoId = string.Empty;

            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(message.ValidateForCurrentState);

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.VideoId), exception.PropertyName);
        }

        [Theory]
        [InlineData(PlayState.Loading)]
        [InlineData(PlayState.Playing)]
        [InlineData(PlayState.Paused)]
        [InlineData(PlayState.Buffering)]
        [InlineData(PlayState.Ended)]
        public void ValidateForCurrentState_WithEmptyTitle_ThrowsUnlessFaulted(PlayState state)
        {
            var message = CreateValid(state);
            message.Title = string.Empty;

            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(message.ValidateForCurrentState);

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.Title), exception.PropertyName);
        }

        [Fact]
        public void ValidateForCurrentState_Faulted_AllowsEmptyVideoIdAndTitle()
        {
            var message = new VideoPlayerChannelFeederMessage
            {
                SessionId = "session-1",
                Host = "Alice",
                State = PlayState.Faulted
            };

            var exception = Record.Exception(message.ValidateForCurrentState);

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(PlayState.Playing)]
        [InlineData(PlayState.Paused)]
        [InlineData(PlayState.Buffering)]
        public void ValidateForCurrentState_ActivelyDecoding_RequiresKnownSourceFrameRate(PlayState state)
        {
            var message = CreateValid(state);
            message.SourceFrameRate = 0;

            var exception = Assert.Throws<VideoPlayerChannelFeederMessageValidationException>(message.ValidateForCurrentState);

            Assert.Equal(nameof(VideoPlayerChannelFeederMessage.SourceFrameRate), exception.PropertyName);
        }

        [Theory]
        [InlineData(PlayState.Loading)]
        [InlineData(PlayState.Ended)]
        public void ValidateForCurrentState_NotActivelyDecoding_AllowsUnknownSourceFrameRate(PlayState state)
        {
            var message = CreateValid(state);
            message.SourceFrameRate = 0;

            var exception = Record.Exception(message.ValidateForCurrentState);

            Assert.Null(exception);
        }
    }
}
