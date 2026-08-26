using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio;
using ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Audio
{
    /// <summary>
    /// Deliberately narrow, mirroring <see cref="FfmpegVideoFrameSourceTests"/>'s own reasoning verbatim:
    /// this repo's CI/dev environment has no native FFmpeg libraries installed, so nothing here can
    /// actually open or decode real audio — real, fixture-based coverage is
    /// <see cref="AudioVideoSyncFixtureTests"/>'s own opt-in scope, to run wherever FFmpeg is actually
    /// available. What <i>is</i> safe to verify anywhere is that constructing this type never eagerly
    /// touches the native libraries — see <see cref="FfmpegAudioFrameSource"/>'s own remarks.
    /// </summary>
    public sealed class FfmpegAudioFrameSourceTests
    {
        [Fact]
        public void Constructor_DoesNotThrow_EvenWithoutNativeFFmpegLibrariesPresent()
        {
            var exception = Record.Exception(() => new FfmpegAudioFrameSource(new FfmpegAudioFrameSourceOptions()));

            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithInvalidMaxChannels_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FfmpegAudioFrameSource(new FfmpegAudioFrameSourceOptions { MaxChannels = 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FfmpegAudioFrameSource(new FfmpegAudioFrameSourceOptions { MaxChannels = 3 }));
        }

        [Fact]
        public async Task ReadFramesAsync_BeforeOpenAsync_Throws()
        {
            await using var source = new FfmpegAudioFrameSource(new FfmpegAudioFrameSourceOptions());

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
                    frame.Dispose();
            });
        }
    }
}
