using System.Diagnostics;
using System.Runtime.CompilerServices;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// #238's own scope, actually applying <c>VideoPlayerChannelConfiguration.SourceOpenTimeout</c> as a
    /// real timeout around <see cref="IVideoFrameSource.OpenAsync"/>.
    /// </summary>
    public sealed class TimeoutVideoFrameSourceTests
    {
        private static readonly VideoSource TestSource = new() { Location = "synthetic://test" };

        [Fact]
        public void Constructor_WithNullInner_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new TimeoutVideoFrameSource(null!, TimeSpan.FromSeconds(1)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithNonPositiveTimeout_Throws(int seconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimeoutVideoFrameSource(new SyntheticVideoFrameSource(), TimeSpan.FromSeconds(seconds)));
        }

        [Fact]
        public async Task OpenAsync_WhenInnerNeverCompletes_ThrowsVideoFrameSourceException_WithinRoughlyTheConfiguredTimeout()
        {
            await using var source = new TimeoutVideoFrameSource(new HangingVideoFrameSource(), TimeSpan.FromMilliseconds(50));
            var stopwatch = Stopwatch.StartNew();

            await Assert.ThrowsAsync<VideoFrameSourceException>(() => source.OpenAsync(TestSource));

            // A generous ceiling, not a tight budget — this only proves the timeout actually bounds the
            // call rather than the inner source's own infinite delay winning, not that it fires at
            // exactly 50ms under CI/load.
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"expected the timeout to fire well under 5s, took {stopwatch.Elapsed}.");
        }

        [Fact]
        public async Task OpenAsync_WhenCallerCancelsBeforeTheTimeout_ThrowsOperationCanceledException_NotVideoFrameSourceException()
        {
            await using var source = new TimeoutVideoFrameSource(new HangingVideoFrameSource(), TimeSpan.FromSeconds(30));
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            var exception = await Record.ExceptionAsync(() => source.OpenAsync(TestSource, cancellationTokenSource.Token));

            Assert.IsAssignableFrom<OperationCanceledException>(exception);
            Assert.IsNotType<VideoFrameSourceException>(exception);
        }

        [Fact]
        public async Task OpenAsync_WhenInnerCompletesWithinTheTimeout_ReturnsTheInnerResult()
        {
            await using var source = new TimeoutVideoFrameSource(new SyntheticVideoFrameSource(), TimeSpan.FromSeconds(30));

            var streamInfo = await source.OpenAsync(TestSource);

            Assert.Equal(SyntheticVideoFrameSource.FrameWidth, streamInfo.Width);
            Assert.Same(streamInfo, source.StreamInfo);
        }

        [Fact]
        public async Task ReadFramesAsync_DelegatesToTheInnerSource()
        {
            var inner = new SyntheticVideoFrameSource();
            await using var source = new TimeoutVideoFrameSource(inner, TimeSpan.FromSeconds(30));
            await source.OpenAsync(TestSource);

            var frameCount = 0;
            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
            {
                frame.Dispose();
                frameCount++;
            }

            Assert.Equal(SyntheticVideoFrameSource.FrameDurations.Count, frameCount);
        }

        [Fact]
        public async Task DisposeAsync_DelegatesToTheInnerSource()
        {
            var inner = new SyntheticVideoFrameSource();
            var source = new TimeoutVideoFrameSource(inner, TimeSpan.FromSeconds(30));

            await source.DisposeAsync();

            Assert.True(inner.Disposed);
        }

        private sealed class HangingVideoFrameSource : IVideoFrameSource
        {
            public VideoStreamInfo? StreamInfo => null;

            public async Task<VideoStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default)
            {
                // Never completes on its own — only ever returns via cancellation, exactly like a real
                // decoder blocked on a slow/unreachable source (see TimeoutVideoFrameSource's own remarks
                // on why this depends on the inner source actually observing cancellation).
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                throw new UnreachableException();
            }

            public async IAsyncEnumerable<DecodedVideoFrame> ReadFramesAsync(TimeSpan startPosition, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.CompletedTask;
                throw new NotSupportedException();
#pragma warning disable CS0162
                yield break;
#pragma warning restore CS0162
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
