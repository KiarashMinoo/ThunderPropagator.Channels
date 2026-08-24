using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Deliberately narrow: this repo's CI/dev environment has no native FFmpeg libraries installed, so
    /// nothing here can actually open or decode real media — #217's own real, fixture-based integration
    /// coverage (frame count/timestamps against a checked-in or generated file) is #237's own scope,
    /// "opt-in FFmpeg integration coverage with a local media fixture," to run wherever FFmpeg is
    /// actually available. What <i>is</i> safe to verify anywhere, regardless of whether the native
    /// libraries are present, is that constructing this type never eagerly touches them — see this
    /// type's own remarks: <c>FFmpeg.AutoGen</c>'s bindings resolve lazily, only on the first actual
    /// native call (i.e. inside <see cref="FfmpegVideoFrameSource.OpenAsync"/>).
    /// </summary>
    public sealed class FfmpegVideoFrameSourceTests
    {
        [Fact]
        public void Constructor_DoesNotThrow_EvenWithoutNativeFFmpegLibrariesPresent()
        {
            var exception = Record.Exception(() => new FfmpegVideoFrameSource(new FfmpegVideoFrameSourceOptions()));

            Assert.Null(exception);
        }

        [Fact]
        public async Task ReadFramesAsync_BeforeOpenAsync_Throws()
        {
            await using var source = new FfmpegVideoFrameSource(new FfmpegVideoFrameSourceOptions());

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (var frame in source.ReadFramesAsync(TimeSpan.Zero))
                    frame.Dispose();
            });
        }
    }
}
