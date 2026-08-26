using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Audio
{
    /// <summary>
    /// Runs the full <see cref="AudioFrameSourceContractTests"/> suite against
    /// <see cref="SyntheticAudioFrameSource"/>, plus tests specific to what makes it useful as a
    /// deterministic fixture: exact, reproducible timing and exactly-once buffer release.
    /// </summary>
    public sealed class SyntheticAudioFrameSourceTests : AudioFrameSourceContractTests
    {
        protected override IAudioFrameSource CreateSource() => new SyntheticAudioFrameSource();

        protected override VideoSource CreateValidSource() => new() { Location = "synthetic://deterministic" };

        [Fact]
        public async Task ReadFramesAsync_FromTheStart_YieldsTheExactDeterministicSequence()
        {
            await using var source = new SyntheticAudioFrameSource();
            await source.OpenAsync(CreateValidSource());

            var expectedPresentationTimestamp = TimeSpan.Zero;
            var chunkIndex = 0;

            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
            {
                try
                {
                    Assert.Equal(expectedPresentationTimestamp, frame.PresentationTimestamp);
                    Assert.Equal(SyntheticAudioFrameSource.ChunkDurations[chunkIndex], frame.Duration);

                    expectedPresentationTimestamp += frame.Duration;
                    chunkIndex++;
                }
                finally
                {
                    frame.Dispose();
                }
            }

            Assert.Equal(SyntheticAudioFrameSource.ChunkDurations.Count, chunkIndex);
        }

        [Fact]
        public async Task ReadFramesAsync_TwoSeparateRuns_ProduceIdenticalTiming()
        {
            await using var first = new SyntheticAudioFrameSource();
            await first.OpenAsync(CreateValidSource());
            var firstRun = new List<(TimeSpan Pts, TimeSpan Duration)>();
            await foreach (var frame in first.ReadFramesAsync(TimeSpan.Zero))
            {
                firstRun.Add((frame.PresentationTimestamp, frame.Duration));
                frame.Dispose();
            }

            await using var second = new SyntheticAudioFrameSource();
            await second.OpenAsync(CreateValidSource());
            var secondRun = new List<(TimeSpan Pts, TimeSpan Duration)>();
            await foreach (var frame in second.ReadFramesAsync(TimeSpan.Zero))
            {
                secondRun.Add((frame.PresentationTimestamp, frame.Duration));
                frame.Dispose();
            }

            Assert.Equal(firstRun, secondRun);
        }

        [Fact]
        public async Task DisposedFrameCount_TracksExactlyOneReleasePerFrame_EvenWhenDisposedRepeatedly()
        {
            await using var source = new SyntheticAudioFrameSource();
            await source.OpenAsync(CreateValidSource());

            var yieldedFrameCount = 0;

            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
            {
                yieldedFrameCount++;
                frame.Dispose();
                frame.Dispose();
            }

            Assert.Equal(yieldedFrameCount, source.DisposedFrameCount);
        }

        [Fact]
        public async Task ReadFramesAsync_EveryFrame_ReportsTheConfiguredSampleRateAndChannels()
        {
            await using var source = new SyntheticAudioFrameSource();
            await source.OpenAsync(CreateValidSource());

            await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
            {
                Assert.Equal(SyntheticAudioFrameSource.SampleRate, frame.SampleRate);
                Assert.Equal(SyntheticAudioFrameSource.Channels, frame.Channels);
                Assert.Equal(AudioSampleFormat.Float32Interleaved, frame.SampleFormat);
                frame.Dispose();
            }
        }
    }
}
