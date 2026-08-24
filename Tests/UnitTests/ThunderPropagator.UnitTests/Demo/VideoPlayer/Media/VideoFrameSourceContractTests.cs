using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// The behavioral contract every <see cref="IVideoFrameSource"/> implementation must satisfy,
    /// regardless of what it decodes or how — #216's own AC: "Contract tests can be reused by concrete
    /// decoder implementations." xUnit discovers <c>[Fact]</c>/<c>[Theory]</c> methods inherited from a
    /// base class through each concrete subclass, so a future decoder's own test class (e.g. #217's
    /// FFmpeg-backed source, or #237's fixture-based integration coverage) gets every test below for
    /// free by deriving from this class and supplying <see cref="CreateSource"/>/
    /// <see cref="CreateValidVideoSource"/> — see <see cref="SyntheticVideoFrameSourceTests"/> for the
    /// reference instantiation against this ticket's own synthetic source.
    /// </summary>
    public abstract class VideoFrameSourceContractTests
    {
        /// <summary>A fresh, unopened source instance for one test.</summary>
        protected abstract IVideoFrameSource CreateSource();

        /// <summary>A <see cref="VideoSource"/> the instance from <see cref="CreateSource"/> can open successfully.</summary>
        protected abstract VideoSource CreateValidVideoSource();

        [Fact]
        public async Task StreamInfo_BeforeOpenAsync_IsNull()
        {
            await using var source = CreateSource();

            Assert.Null(source.StreamInfo);
        }

        [Fact]
        public async Task OpenAsync_PopulatesStreamInfo_MatchingItsOwnReturnValue()
        {
            await using var source = CreateSource();

            var returned = await source.OpenAsync(CreateValidVideoSource());

            Assert.Equal(returned, source.StreamInfo);
        }

        [Fact]
        public async Task ReadFramesAsync_BeforeOpenAsync_Throws()
        {
            await using var source = CreateSource();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
                    frame.Dispose();
            });
        }

        [Fact]
        public async Task OpenAsync_WithAlreadyCancelledToken_ThrowsPromptly()
        {
            await using var source = CreateSource();
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => source.OpenAsync(CreateValidVideoSource(), cancellationTokenSource.Token));
        }

        [Fact]
        public async Task ReadFramesAsync_WithAlreadyCancelledToken_ThrowsBeforeYieldingAnyFrame()
        {
            await using var source = CreateSource();
            await source.OpenAsync(CreateValidVideoSource());

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero, cancellationTokenSource.Token))
                    frame.Dispose();
            });
        }

        [Fact]
        public async Task ReadFramesAsync_YieldsFramesInNonDecreasingPresentationOrder()
        {
            await using var source = CreateSource();
            await source.OpenAsync(CreateValidVideoSource());

            TimeSpan? previousPresentationTimestamp = null;

            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
            {
                try
                {
                    if (previousPresentationTimestamp is not null)
                        Assert.True(frame.PresentationTimestamp >= previousPresentationTimestamp.Value);

                    previousPresentationTimestamp = frame.PresentationTimestamp;
                }
                finally
                {
                    frame.Dispose();
                }
            }

            Assert.NotNull(previousPresentationTimestamp);
        }

        [Fact]
        public async Task DecodedVideoFrame_Dispose_IsSafeToCallMoreThanOnce()
        {
            await using var source = CreateSource();
            await source.OpenAsync(CreateValidVideoSource());

            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
            {
                frame.Dispose();
                var exception = Record.Exception(frame.Dispose);

                Assert.Null(exception);
                break;
            }
        }
    }
}
