using SkiaSharp;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// #237's own scope, "opt-in FFmpeg integration coverage with a local media fixture" — real
    /// <see cref="FfmpegVideoFrameSource"/> decode/encode/seek/cancel/dispose against two locally
    /// generated fixtures (never checked in, never fetched from the internet — see
    /// <c>Fixtures/README.md</c> for provenance and how to generate them). Every test here is a no-op
    /// pass unless its own environment variable points at an existing file, mirroring
    /// <see cref="ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Session.AudioVideoSyncFixtureTests"/>'s
    /// own established opt-in pattern (that one already covers A/V timebase sharing across both tracks
    /// of the CFR fixture; this covers the video-only decode/encode/seek/cancel/dispose contract against
    /// both the CFR and VFR fixtures).
    /// </summary>
    public sealed class FfmpegVideoFrameSourceFixtureTests
    {
        /// <summary>Environment variable naming a local constant-frame-rate media file to run the CFR-specific cases against.</summary>
        public const string CfrFixtureEnvironmentVariable = "THUNDERPROPAGATOR_VIDEOPLAYER_CFR_FIXTURE";

        /// <summary>Environment variable naming a local variable-frame-rate media file to run the VFR-specific cases against.</summary>
        public const string VfrFixtureEnvironmentVariable = "THUNDERPROPAGATOR_VIDEOPLAYER_VFR_FIXTURE";

        private static string? GetFixturePathOrNull(string environmentVariable)
        {
            var path = Environment.GetEnvironmentVariable(environmentVariable);
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? path : null;
        }

        [Fact]
        public async Task OpenAsync_CfrFixture_PopulatesConstantFrameRateStreamInfo()
        {
            var fixturePath = GetFixturePathOrNull(CfrFixtureEnvironmentVariable);
            if (fixturePath is null)
                return; // opted out — see this type's own remarks.

            await using var source = new FfmpegVideoFrameSource(new FfmpegVideoFrameSourceOptions());
            var streamInfo = await source.OpenAsync(new VideoSource { Location = fixturePath });

            Assert.False(streamInfo.IsVariableFrameRate);
            Assert.True(streamInfo.Width > 0);
            Assert.True(streamInfo.Height > 0);
            Assert.True(streamInfo.Duration > TimeSpan.Zero);
        }

        [Fact]
        public async Task OpenAsync_VfrFixture_PopulatesVariableFrameRateStreamInfo()
        {
            var fixturePath = GetFixturePathOrNull(VfrFixtureEnvironmentVariable);
            if (fixturePath is null)
                return;

            await using var source = new FfmpegVideoFrameSource(new FfmpegVideoFrameSourceOptions());
            var streamInfo = await source.OpenAsync(new VideoSource { Location = fixturePath });

            Assert.True(streamInfo.IsVariableFrameRate);
        }

        [Fact]
        public async Task ReadFramesAsync_CfrFixture_YieldsNonDecreasingTimestamps_WithApproximatelyTheExpectedFrameCount()
        {
            var fixturePath = GetFixturePathOrNull(CfrFixtureEnvironmentVariable);
            if (fixturePath is null)
                return;

            await using var source = new FfmpegVideoFrameSource(new FfmpegVideoFrameSourceOptions());
            var streamInfo = await source.OpenAsync(new VideoSource { Location = fixturePath });

            var frameCount = 0;
            TimeSpan? previousPts = null;
            TimeSpan lastPts = TimeSpan.Zero;

            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
            {
                try
                {
                    if (previousPts is not null)
                        Assert.True(frame.PresentationTimestamp >= previousPts.Value);

                    previousPts = frame.PresentationTimestamp;
                    lastPts = frame.PresentationTimestamp;
                    frameCount++;
                }
                finally
                {
                    frame.Dispose();
                }
            }

            // The generator script produces 25fps for 2 seconds (50 frames) — a wide tolerance here
            // (rather than an exact 50) because encoder GOP/flush behavior can legitimately shift the
            // exact count by a frame or two without indicating anything wrong.
            Assert.InRange(frameCount, 40, 60);
            Assert.True(lastPts <= streamInfo.Duration + TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ReadFramesAsync_VfrFixture_YieldsNonDecreasingTimestamps()
        {
            var fixturePath = GetFixturePathOrNull(VfrFixtureEnvironmentVariable);
            if (fixturePath is null)
                return;

            await using var source = new FfmpegVideoFrameSource(new FfmpegVideoFrameSourceOptions());
            await source.OpenAsync(new VideoSource { Location = fixturePath });

            var frameCount = 0;
            TimeSpan? previousPts = null;

            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
            {
                try
                {
                    if (previousPts is not null)
                        Assert.True(frame.PresentationTimestamp >= previousPts.Value);

                    previousPts = frame.PresentationTimestamp;
                    frameCount++;
                }
                finally
                {
                    frame.Dispose();
                }
            }

            Assert.True(frameCount > 0);
        }

        [Fact]
        public async Task ReadFramesAsync_ThenEncoded_ProducesAnIndependentlyDecodableImage()
        {
            var fixturePath = GetFixturePathOrNull(CfrFixtureEnvironmentVariable);
            if (fixturePath is null)
                return;

            await using var source = new FfmpegVideoFrameSource(new FfmpegVideoFrameSourceOptions());
            await source.OpenAsync(new VideoSource { Location = fixturePath });

            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
            {
                var expectedWidth = frame.Width;
                var expectedHeight = frame.Height;

                var encoded = VideoFrameEncoder.Encode(frame, VideoFramePacketEncoding.Jpeg, quality: 90);
                frame.Dispose();

                Assert.NotEmpty(encoded.ToArray());

                using var decoded = SKBitmap.Decode(encoded.ToArray());

                Assert.NotNull(decoded);
                Assert.Equal(expectedWidth, decoded.Width);
                Assert.Equal(expectedHeight, decoded.Height);
                return;
            }

            Assert.Fail("The CFR fixture produced no frames to encode.");
        }

        // #236/#237's own "epoch restart"/re-seek scenario against the real decoder — IVideoFrameSource's
        // own contract ("calling this again, including mid-enumeration of a previous call, is how a
        // caller re-seeks; an implementation must abandon any prior enumeration's in-progress decode
        // state cleanly") verified against FfmpegVideoFrameSource specifically, not just the synthetic
        // source VideoFrameSourceContractTests already exercises this against.
        [Fact]
        public async Task ReadFramesAsync_CalledAgainMidEnumeration_AbandonsThePriorEnumeration_AndRestartsNearTheNewPosition()
        {
            var fixturePath = GetFixturePathOrNull(CfrFixtureEnvironmentVariable);
            if (fixturePath is null)
                return;

            await using var source = new FfmpegVideoFrameSource(new FfmpegVideoFrameSourceOptions());
            var streamInfo = await source.OpenAsync(new VideoSource { Location = fixturePath });

            await using var firstEnumerator = source.ReadFramesAsync(TimeSpan.Zero).GetAsyncEnumerator();
            Assert.True(await firstEnumerator.MoveNextAsync());
            firstEnumerator.Current.Dispose();

            var seekPosition = TimeSpan.FromTicks(streamInfo.Duration.Ticks / 2);
            DecodedVideoFrame? firstFrameAfterSeek = null;
            var framesAfterSeek = 0;

            await foreach (var frame in source.ReadFramesAsync(seekPosition))
            {
                firstFrameAfterSeek ??= frame;
                framesAfterSeek++;

                if (framesAfterSeek >= 3)
                {
                    frame.Dispose();
                    break;
                }

                if (!ReferenceEquals(frame, firstFrameAfterSeek))
                    frame.Dispose();
            }

            // The superseded first enumeration must never produce another frame after being abandoned —
            // this is the actual "epoch restart" contract, not merely that the new call itself works.
            Assert.False(await firstEnumerator.MoveNextAsync());

            Assert.NotNull(firstFrameAfterSeek);
            // av_seek_frame(AVSEEK_FLAG_BACKWARD) lands on the nearest keyframe at or before the request,
            // so this only asserts "clearly restarted near the seek point," not an exact match — a tight
            // equality would be flaky against this fixture's own GOP structure.
            Assert.True(firstFrameAfterSeek!.PresentationTimestamp > TimeSpan.Zero);
            Assert.True(firstFrameAfterSeek.PresentationTimestamp < seekPosition + TimeSpan.FromSeconds(1));

            firstFrameAfterSeek.Dispose();
        }

        [Fact]
        public async Task ReadFramesAsync_CancelledMidDecode_ThrowsOperationCanceledException_AndSourceCanStillBeDisposedCleanly()
        {
            var fixturePath = GetFixturePathOrNull(CfrFixtureEnvironmentVariable);
            if (fixturePath is null)
                return;

            var source = new FfmpegVideoFrameSource(new FfmpegVideoFrameSourceOptions());
            await source.OpenAsync(new VideoSource { Location = fixturePath });

            using var cancellationTokenSource = new CancellationTokenSource();
            var framesSeen = 0;

            var exception = await Record.ExceptionAsync(async () =>
            {
                await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero, cancellationTokenSource.Token))
                {
                    frame.Dispose();
                    framesSeen++;

                    if (framesSeen == 2)
                        await cancellationTokenSource.CancelAsync();
                }
            });

            Assert.IsAssignableFrom<OperationCanceledException>(exception);

            var disposeException = await Record.ExceptionAsync(async () => await source.DisposeAsync());
            Assert.Null(disposeException);
        }

        [Fact]
        public async Task OpenAsync_WithAnInvalidPath_ThrowsVideoFrameSourceException_AndSourceCanStillBeDisposedCleanly()
        {
            // Doesn't need the fixture file itself, only real native FFmpeg — gated the same way as
            // every other test here for consistency ("no FFmpeg available" would otherwise fail this
            // differently than the AC's own "skip with a clear reason" intent).
            if (GetFixturePathOrNull(CfrFixtureEnvironmentVariable) is null)
                return;

            var source = new FfmpegVideoFrameSource(new FfmpegVideoFrameSourceOptions());

            await Assert.ThrowsAsync<VideoFrameSourceException>(
                () => source.OpenAsync(new VideoSource { Location = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.mp4") }));

            var disposeException = await Record.ExceptionAsync(async () => await source.DisposeAsync());
            Assert.Null(disposeException);
        }

        [Fact]
        public async Task DisposeAsync_AfterFullyConsumingTheStream_IsSafeToCallMoreThanOnce()
        {
            var fixturePath = GetFixturePathOrNull(CfrFixtureEnvironmentVariable);
            if (fixturePath is null)
                return;

            var source = new FfmpegVideoFrameSource(new FfmpegVideoFrameSourceOptions());
            await source.OpenAsync(new VideoSource { Location = fixturePath });

            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
                frame.Dispose();

            var first = await Record.ExceptionAsync(async () => await source.DisposeAsync());
            var second = await Record.ExceptionAsync(async () => await source.DisposeAsync());

            Assert.Null(first);
            Assert.Null(second);
        }
    }
}
