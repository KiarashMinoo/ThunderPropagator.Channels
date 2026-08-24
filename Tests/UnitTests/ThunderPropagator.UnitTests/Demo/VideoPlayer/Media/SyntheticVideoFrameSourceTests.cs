using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Runs the full <see cref="VideoFrameSourceContractTests"/> suite against
    /// <see cref="SyntheticVideoFrameSource"/>, plus tests specific to what makes it useful as a
    /// deterministic VFR fixture: exact, reproducible timing and exactly-once buffer release.
    /// </summary>
    public sealed class SyntheticVideoFrameSourceTests : VideoFrameSourceContractTests
    {
        protected override IVideoFrameSource CreateSource() => new SyntheticVideoFrameSource();

        protected override VideoSource CreateValidVideoSource() => new() { Location = "synthetic://deterministic" };

        [Fact]
        public async Task ReadFramesAsync_FromTheStart_YieldsTheExactDeterministicSequence()
        {
            await using var source = new SyntheticVideoFrameSource();
            await source.OpenAsync(CreateValidVideoSource());

            var expectedPresentationTimestamp = TimeSpan.Zero;
            var frameIndex = 0;

            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
            {
                try
                {
                    Assert.Equal(expectedPresentationTimestamp, frame.PresentationTimestamp);
                    Assert.Equal(SyntheticVideoFrameSource.FrameDurations[frameIndex], frame.Duration);

                    expectedPresentationTimestamp += frame.Duration;
                    frameIndex++;
                }
                finally
                {
                    frame.Dispose();
                }
            }

            Assert.Equal(SyntheticVideoFrameSource.FrameDurations.Count, frameIndex);
        }

        [Fact]
        public async Task ReadFramesAsync_ProducesAtLeastTwoDistinctDurations_ProvingItIsVariableFrameRate()
        {
            await using var source = new SyntheticVideoFrameSource();
            await source.OpenAsync(CreateValidVideoSource());

            var distinctDurations = new HashSet<TimeSpan>();

            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
            {
                distinctDurations.Add(frame.Duration);
                frame.Dispose();
            }

            Assert.True(distinctDurations.Count > 1, "A variable-frame-rate source must not produce a single constant duration.");
        }

        [Fact]
        public async Task OpenAsync_ReportsIsVariableFrameRateTrue()
        {
            await using var source = new SyntheticVideoFrameSource();

            var streamInfo = await source.OpenAsync(CreateValidVideoSource());

            Assert.True(streamInfo.IsVariableFrameRate);
        }

        [Fact]
        public async Task ReadFramesAsync_TwoSeparateRuns_ProduceIdenticalTiming()
        {
            // Determinism, not just variability: the exact same sequence must come back every time.
            await using var first = new SyntheticVideoFrameSource();
            await first.OpenAsync(CreateValidVideoSource());
            var firstRun = new List<(TimeSpan Pts, TimeSpan Duration)>();
            await foreach (var frame in first.ReadFramesAsync(TimeSpan.Zero))
            {
                firstRun.Add((frame.PresentationTimestamp, frame.Duration));
                frame.Dispose();
            }

            await using var second = new SyntheticVideoFrameSource();
            await second.OpenAsync(CreateValidVideoSource());
            var secondRun = new List<(TimeSpan Pts, TimeSpan Duration)>();
            await foreach (var frame in second.ReadFramesAsync(TimeSpan.Zero))
            {
                secondRun.Add((frame.PresentationTimestamp, frame.Duration));
                frame.Dispose();
            }

            Assert.Equal(firstRun, secondRun);
        }

        [Theory]
        [InlineData(74, 2)] // exactly frame index 2's own start (durations 33+41=74 elapsed)
        [InlineData(80, 2)] // mid-way through frame index 2's own display window [74,107)
        [InlineData(107, 3)] // exactly frame index 3's own start (33+41+33=107 elapsed)
        public async Task ReadFramesAsync_FromAMidStreamPosition_SkipsFramesThatHaveFullyElapsed(int startPositionMilliseconds, int expectedFirstFrameIndex)
        {
            await using var source = new SyntheticVideoFrameSource();
            await source.OpenAsync(CreateValidVideoSource());

            var expectedPresentationTimestamp = SyntheticVideoFrameSource.FrameDurations
                .Take(expectedFirstFrameIndex)
                .Aggregate(TimeSpan.Zero, (total, duration) => total + duration);

            await foreach (var frame in source.ReadFramesAsync(TimeSpan.FromMilliseconds(startPositionMilliseconds)))
            {
                Assert.Equal(expectedPresentationTimestamp, frame.PresentationTimestamp);
                frame.Dispose();
                return;
            }

            Assert.Fail("Expected at least one frame to be yielded.");
        }

        [Fact]
        public async Task DisposedFrameCount_TracksExactlyOneReleasePerFrame_EvenWhenDisposedRepeatedly()
        {
            await using var source = new SyntheticVideoFrameSource();
            await source.OpenAsync(CreateValidVideoSource());

            var yieldedFrameCount = 0;

            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
            {
                yieldedFrameCount++;
                frame.Dispose();
                frame.Dispose(); // repeated dispose must not double-count the release
            }

            Assert.Equal(yieldedFrameCount, source.DisposedFrameCount);
        }
    }
}
