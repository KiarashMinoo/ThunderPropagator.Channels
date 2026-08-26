using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Deliberately narrow, mirroring <see cref="FfmpegVideoFrameSourceTests"/>'s own reasoning: this
    /// repo's CI/dev environment has no native FFmpeg libraries installed, so nothing here can actually
    /// open a real Opus encoder — real, fixture-based coverage is <see cref="AudioVideoSyncFixtureTests"/>'s
    /// own opt-in scope. What <i>is</i> safe to verify anywhere is argument validation and that
    /// construction never eagerly touches the native libraries — see <see cref="AudioFrameEncoder"/>'s
    /// own remarks on its lazy-open design.
    /// </summary>
    public sealed class AudioFrameEncoderTests
    {
        [Fact]
        public void Constructor_DoesNotThrow_EvenWithoutNativeFFmpegLibrariesPresent()
        {
            var exception = Record.Exception(() => new AudioFrameEncoder(48_000, 2));

            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithInvalidSampleRate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AudioFrameEncoder(0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AudioFrameEncoder(-1, 2));
        }

        [Fact]
        public void Constructor_WithInvalidChannels_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AudioFrameEncoder(48_000, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AudioFrameEncoder(48_000, 3));
        }

        [Fact]
        public void Constructor_WithInvalidBitRate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AudioFrameEncoder(48_000, 2, bitRate: 0));
        }

        [Fact]
        public void Constructor_WithInvalidEncoding_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AudioFrameEncoder(48_000, 2, (AudioFramePacketEncoding)255));
        }

        [Fact]
        public void Constructor_WithAac_DoesNotThrow_EvenWithoutNativeFFmpegLibrariesPresent()
        {
            var exception = Record.Exception(() => new AudioFrameEncoder(48_000, 2, AudioFramePacketEncoding.Aac));

            Assert.Null(exception);
        }

        [Fact]
        public void Encoding_DefaultsToOpus()
        {
            using var encoder = new AudioFrameEncoder(48_000, 2);

            Assert.Equal(AudioFramePacketEncoding.Opus, encoder.Encoding);
        }

        [Fact]
        public void Encoding_ReflectsWhatWasConfigured()
        {
            using var encoder = new AudioFrameEncoder(48_000, 2, AudioFramePacketEncoding.Aac);

            Assert.Equal(AudioFramePacketEncoding.Aac, encoder.Encoding);
        }

        [Fact]
        public void FrameSize_BeforeFirstUse_IsZero()
        {
            using var encoder = new AudioFrameEncoder(48_000, 2);

            Assert.Equal(0, encoder.FrameSize);
        }

        [Fact]
        public void Dispose_BeforeFirstUse_IsSafe()
        {
            var encoder = new AudioFrameEncoder(48_000, 2);

            var exception = Record.Exception(encoder.Dispose);

            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_IsSafeToCallMoreThanOnce()
        {
            var encoder = new AudioFrameEncoder(48_000, 2);
            encoder.Dispose();

            var exception = Record.Exception(encoder.Dispose);

            Assert.Null(exception);
        }
    }
}
