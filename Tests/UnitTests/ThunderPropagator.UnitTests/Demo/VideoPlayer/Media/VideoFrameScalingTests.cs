using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Issue #217's own scope: "Scale to configured target bounds while preserving aspect ratio."
    /// </summary>
    public sealed class VideoFrameScalingTests
    {
        [Fact]
        public void ComputeScaledSize_SourceSmallerThanBounds_IsNotUpscaled()
        {
            var (width, height) = VideoFrameScaling.ComputeScaledSize(640, 360, 1280, 720);

            Assert.Equal(640, width);
            Assert.Equal(360, height);
        }

        [Fact]
        public void ComputeScaledSize_SourceWiderThanBounds_ScalesDownPreservingAspectRatio()
        {
            var (width, height) = VideoFrameScaling.ComputeScaledSize(3840, 2160, 1280, 720);

            Assert.Equal(1280, width);
            Assert.Equal(720, height);
        }

        [Fact]
        public void ComputeScaledSize_SourceAspectRatioNarrowerThanBounds_IsLimitedByHeight()
        {
            // A very tall source (portrait) hits the height bound before the width bound.
            var (width, height) = VideoFrameScaling.ComputeScaledSize(1080, 1920, 1280, 720);

            Assert.Equal(720, height);
            Assert.True(width < 1280);
            Assert.Equal(1080.0 / 1920.0, (double)width / height, 2);
        }

        [Theory]
        [InlineData(1281, 720)]
        [InlineData(1280, 721)]
        [InlineData(101, 101)]
        public void ComputeScaledSize_AlwaysReturnsEvenDimensions(int sourceWidth, int sourceHeight)
        {
            var (width, height) = VideoFrameScaling.ComputeScaledSize(sourceWidth, sourceHeight, 1280, 720);

            Assert.Equal(0, width % 2);
            Assert.Equal(0, height % 2);
        }

        [Fact]
        public void ComputeScaledSize_NeverReturnsBelowTwoInEitherDimension()
        {
            var (width, height) = VideoFrameScaling.ComputeScaledSize(10_000, 1, 4, 4);

            Assert.True(width >= 2);
            Assert.True(height >= 2);
        }

        [Theory]
        [InlineData(0, 100, 100, 100)]
        [InlineData(100, 0, 100, 100)]
        [InlineData(100, 100, 0, 100)]
        [InlineData(100, 100, 100, 0)]
        [InlineData(-1, 100, 100, 100)]
        public void ComputeScaledSize_WithNonPositiveArgument_Throws(int sourceWidth, int sourceHeight, int maxWidth, int maxHeight)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => VideoFrameScaling.ComputeScaledSize(sourceWidth, sourceHeight, maxWidth, maxHeight));
        }
    }
}
