using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// The behavioral contract every <see cref="IAudioFrameSource"/> implementation must satisfy —
    /// mirrors <see cref="VideoFrameSourceContractTests"/>'s own role for the video side. A future
    /// decoder's own test class gets every test below for free by deriving from this class — see
    /// <see cref="SyntheticAudioFrameSourceTests"/> for the reference instantiation.
    /// </summary>
    public abstract class AudioFrameSourceContractTests
    {
        /// <summary>A fresh, unopened source instance for one test.</summary>
        protected abstract IAudioFrameSource CreateSource();

        /// <summary>A <see cref="VideoSource"/> the instance from <see cref="CreateSource"/> can open successfully.</summary>
        protected abstract VideoSource CreateValidSource();

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

            var returned = await source.OpenAsync(CreateValidSource());

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
                () => source.OpenAsync(CreateValidSource(), cancellationTokenSource.Token));
        }

        [Fact]
        public async Task ReadFramesAsync_WithAlreadyCancelledToken_ThrowsBeforeYieldingAnyFrame()
        {
            await using var source = CreateSource();
            await source.OpenAsync(CreateValidSource());

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
            await source.OpenAsync(CreateValidSource());

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
        public async Task DecodedAudioFrame_Dispose_IsSafeToCallMoreThanOnce()
        {
            await using var source = CreateSource();
            await source.OpenAsync(CreateValidSource());

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
