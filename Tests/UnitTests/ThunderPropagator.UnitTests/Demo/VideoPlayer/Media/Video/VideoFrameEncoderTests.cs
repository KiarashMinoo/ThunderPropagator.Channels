using SkiaSharp;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// Issue #217's own scope: "Encode each output frame as JPEG or WebP using configured quality." —
    /// exercised end to end via SkiaSharp alone (no FFmpeg dependency at all), so these tests genuinely
    /// run in any environment: encode a synthetic frame, then decode the result back and check it.
    /// </summary>
    public sealed class VideoFrameEncoderTests
    {
        private const int FrameWidth = 16;
        private const int FrameHeight = 16;

        private static DecodedVideoFrame CreateSolidColorFrame(VideoPixelFormat pixelFormat, byte b, byte g, byte r, byte a = 255)
        {
            var data = new byte[FrameWidth * FrameHeight * 4];

            for (var pixel = 0; pixel < FrameWidth * FrameHeight; pixel++)
            {
                var offset = pixel * 4;
                if (pixelFormat == VideoPixelFormat.Bgra32)
                {
                    data[offset] = b;
                    data[offset + 1] = g;
                    data[offset + 2] = r;
                    data[offset + 3] = a;
                }
                else
                {
                    data[offset] = r;
                    data[offset + 1] = g;
                    data[offset + 2] = b;
                    data[offset + 3] = a;
                }
            }

            return new DecodedVideoFrame(TimeSpan.Zero, TimeSpan.FromMilliseconds(33), FrameWidth, FrameHeight, pixelFormat, data);
        }

        [Theory]
        [InlineData(VideoFramePacketEncoding.Jpeg)]
        [InlineData(VideoFramePacketEncoding.WebP)]
        public void Encode_ProducesBytesThatDecodeBackToTheOriginalDimensions(VideoFramePacketEncoding encoding)
        {
            var frame = CreateSolidColorFrame(VideoPixelFormat.Bgra32, b: 40, g: 120, r: 200);

            var encoded = VideoFrameEncoder.Encode(frame, encoding, quality: 90);

            using var decoded = SKBitmap.Decode(encoded.ToArray());

            Assert.NotNull(decoded);
            Assert.Equal(FrameWidth, decoded.Width);
            Assert.Equal(FrameHeight, decoded.Height);
        }

        [Theory]
        [InlineData(VideoFramePacketEncoding.Jpeg)]
        [InlineData(VideoFramePacketEncoding.WebP)]
        public void Encode_PreservesApproximateColor_ForBgra32Input(VideoFramePacketEncoding encoding)
        {
            var frame = CreateSolidColorFrame(VideoPixelFormat.Bgra32, b: 40, g: 120, r: 200);

            var encoded = VideoFrameEncoder.Encode(frame, encoding, quality: 95);

            using var decoded = SKBitmap.Decode(encoded.ToArray());
            var pixel = decoded.GetPixel(FrameWidth / 2, FrameHeight / 2);

            // Lossy compression at high quality should stay within a small tolerance of the original.
            Assert.InRange(pixel.Red, 190, 210);
            Assert.InRange(pixel.Green, 110, 130);
            Assert.InRange(pixel.Blue, 30, 50);
        }

        [Fact]
        public void Encode_PreservesApproximateColor_ForRgba32Input()
        {
            var frame = CreateSolidColorFrame(VideoPixelFormat.Rgba32, b: 40, g: 120, r: 200);

            var encoded = VideoFrameEncoder.Encode(frame, VideoFramePacketEncoding.Jpeg, quality: 95);

            using var decoded = SKBitmap.Decode(encoded.ToArray());
            var pixel = decoded.GetPixel(FrameWidth / 2, FrameHeight / 2);

            Assert.InRange(pixel.Red, 190, 210);
            Assert.InRange(pixel.Green, 110, 130);
            Assert.InRange(pixel.Blue, 30, 50);
        }

        [Fact]
        public void Encode_HigherQuality_ProducesLargerOrEqualOutput()
        {
            var frame = CreateSolidColorFrame(VideoPixelFormat.Bgra32, b: 12, g: 200, r: 77);

            var lowQuality = VideoFrameEncoder.Encode(frame, VideoFramePacketEncoding.Jpeg, quality: 5);
            var highQuality = VideoFrameEncoder.Encode(frame, VideoFramePacketEncoding.Jpeg, quality: 95);

            Assert.True(highQuality.Length >= lowQuality.Length);
        }

        [Theory]
        [InlineData(VideoPixelFormat.Yuv420P)]
        [InlineData(VideoPixelFormat.Nv12)]
        [InlineData(VideoPixelFormat.Rgb24)]
        [InlineData(VideoPixelFormat.Bgr24)]
        public void Encode_WithUnsupportedPixelFormat_Throws(VideoPixelFormat pixelFormat)
        {
            var frame = new DecodedVideoFrame(TimeSpan.Zero, TimeSpan.Zero, FrameWidth, FrameHeight, pixelFormat, new byte[FrameWidth * FrameHeight * 4]);

            Assert.Throws<NotSupportedException>(() => VideoFrameEncoder.Encode(frame, VideoFramePacketEncoding.Jpeg, quality: 80));
        }

        [Fact]
        public void Encode_WithUndefinedEncoding_Throws()
        {
            var frame = CreateSolidColorFrame(VideoPixelFormat.Bgra32, 0, 0, 0);

            Assert.Throws<NotSupportedException>(() => VideoFrameEncoder.Encode(frame, (VideoFramePacketEncoding)byte.MaxValue, quality: 80));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Encode_WithQualityOutOfRange_Throws(int quality)
        {
            var frame = CreateSolidColorFrame(VideoPixelFormat.Bgra32, 0, 0, 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => VideoFrameEncoder.Encode(frame, VideoFramePacketEncoding.Jpeg, quality));
        }
    }
}
