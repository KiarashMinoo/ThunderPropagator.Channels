using System.Diagnostics;
using System.Runtime.CompilerServices;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Audio
{
    /// <summary>The audio-side counterpart to <see cref="TimeoutVideoFrameSourceTests"/> — see that type's own remarks.</summary>
    public sealed class TimeoutAudioFrameSourceTests
    {
        private static readonly VideoSource TestSource = new() { Location = "synthetic://test" };

        [Fact]
        public void Constructor_WithNullInner_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new TimeoutAudioFrameSource(null!, TimeSpan.FromSeconds(1)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithNonPositiveTimeout_Throws(int seconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimeoutAudioFrameSource(new SyntheticAudioFrameSource(), TimeSpan.FromSeconds(seconds)));
        }

        [Fact]
        public async Task OpenAsync_WhenInnerNeverCompletes_ThrowsVideoFrameSourceException_WithinRoughlyTheConfiguredTimeout()
        {
            await using var source = new TimeoutAudioFrameSource(new HangingAudioFrameSource(), TimeSpan.FromMilliseconds(50));
            var stopwatch = Stopwatch.StartNew();

            await Assert.ThrowsAsync<VideoFrameSourceException>(() => source.OpenAsync(TestSource));

            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"expected the timeout to fire well under 5s, took {stopwatch.Elapsed}.");
        }

        [Fact]
        public async Task OpenAsync_WhenCallerCancelsBeforeTheTimeout_ThrowsOperationCanceledException_NotVideoFrameSourceException()
        {
            await using var source = new TimeoutAudioFrameSource(new HangingAudioFrameSource(), TimeSpan.FromSeconds(30));
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            var exception = await Record.ExceptionAsync(() => source.OpenAsync(TestSource, cancellationTokenSource.Token));

            Assert.IsAssignableFrom<OperationCanceledException>(exception);
            Assert.IsNotType<VideoFrameSourceException>(exception);
        }

        [Fact]
        public async Task OpenAsync_WhenInnerCompletesWithinTheTimeout_ReturnsTheInnerResult()
        {
            await using var source = new TimeoutAudioFrameSource(new SyntheticAudioFrameSource(), TimeSpan.FromSeconds(30));

            var streamInfo = await source.OpenAsync(TestSource);

            Assert.Equal(SyntheticAudioFrameSource.SampleRate, streamInfo.SampleRate);
            Assert.Same(streamInfo, source.StreamInfo);
        }

        [Fact]
        public async Task ReadFramesAsync_DelegatesToTheInnerSource()
        {
            var inner = new SyntheticAudioFrameSource();
            await using var source = new TimeoutAudioFrameSource(inner, TimeSpan.FromSeconds(30));
            await source.OpenAsync(TestSource);

            var frameCount = 0;
            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
            {
                frame.Dispose();
                frameCount++;
            }

            Assert.Equal(SyntheticAudioFrameSource.ChunkDurations.Count, frameCount);
        }

        [Fact]
        public async Task DisposeAsync_DelegatesToTheInnerSource()
        {
            var inner = new SyntheticAudioFrameSource();
            var source = new TimeoutAudioFrameSource(inner, TimeSpan.FromSeconds(30));

            await source.DisposeAsync();

            Assert.True(inner.Disposed);
        }

        private sealed class HangingAudioFrameSource : IAudioFrameSource
        {
            public AudioStreamInfo? StreamInfo => null;

            public async Task<AudioStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                throw new UnreachableException();
            }

            public async IAsyncEnumerable<DecodedAudioFrame> ReadFramesAsync(TimeSpan startPosition, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
