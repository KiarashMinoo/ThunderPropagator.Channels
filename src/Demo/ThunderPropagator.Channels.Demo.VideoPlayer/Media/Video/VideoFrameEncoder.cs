using SkiaSharp;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// Compresses an already-decoded (and, for <see cref="FfmpegVideoFrameSource"/>, already-scaled)
    /// <see cref="DecodedVideoFrame"/> into JPEG or WebP bytes ready to carry as a
    /// <see cref="VideoFramePacket.Payload"/> — #217's own scope: "Encode each output frame as JPEG or
    /// WebP using configured quality." Deliberately independent of any <see cref="IVideoFrameSource"/>
    /// implementation (including FFmpeg) — it only operates on a frame's already-decoded pixels, via
    /// SkiaSharp, so it can be exercised in tests without any native FFmpeg dependency at all.
    /// </summary>
    public static class VideoFrameEncoder
    {
        /// <summary>Minimum accepted <c>quality</c> value.</summary>
        public const int MinQuality = 0;

        /// <summary>Maximum accepted <c>quality</c> value.</summary>
        public const int MaxQuality = 100;

        /// <summary>
        /// Encodes <paramref name="frame"/>'s pixels as <paramref name="encoding"/> at
        /// <paramref name="quality"/> (0 = smallest/lowest fidelity, 100 = largest/highest fidelity).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> is outside <see cref="MinQuality"/>..<see cref="MaxQuality"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="frame"/>'s <see cref="DecodedVideoFrame.PixelFormat"/> is not
        /// <see cref="VideoPixelFormat.Rgba32"/> or <see cref="VideoPixelFormat.Bgra32"/> (the only
        /// formats this encoder reads directly, with no color-space conversion of its own — a decoder
        /// producing another format, such as <see cref="FfmpegVideoFrameSource"/>'s own source-native
        /// YUV, must convert before handing frames to this encoder, exactly as
        /// <see cref="FfmpegVideoFrameSource"/> itself does via <c>libswscale</c>), or
        /// <paramref name="encoding"/> is not a value this encoder recognizes.
        /// </exception>
        public static ReadOnlyMemory<byte> Encode(DecodedVideoFrame frame, VideoFramePacketEncoding encoding, int quality)
        {
            ArgumentNullException.ThrowIfNull(frame);

            if (quality < MinQuality || quality > MaxQuality)
                throw new ArgumentOutOfRangeException(nameof(quality), quality, $"must be between {MinQuality} and {MaxQuality}.");

            var colorType = frame.PixelFormat switch
            {
                VideoPixelFormat.Rgba32 => SKColorType.Rgba8888,
                VideoPixelFormat.Bgra32 => SKColorType.Bgra8888,
                _ => throw new NotSupportedException($"{frame.PixelFormat} is not supported for encoding directly — convert to Rgba32 or Bgra32 first.")
            };

            var format = encoding switch
            {
                VideoFramePacketEncoding.Jpeg => SKEncodedImageFormat.Jpeg,
                VideoFramePacketEncoding.WebP => SKEncodedImageFormat.Webp,
                _ => throw new NotSupportedException($"{encoding} is not a supported encoding.")
            };

            var imageInfo = new SKImageInfo(frame.Width, frame.Height, colorType, SKAlphaType.Unpremul);

            using var pinnedPixels = frame.Data.Pin();
            using var bitmap = new SKBitmap();

            unsafe
            {
                if (!bitmap.InstallPixels(imageInfo, (nint)pinnedPixels.Pointer, frame.Width * 4))
                    throw new InvalidOperationException("Wrapping the decoded frame's pixel buffer for encoding failed.");
            }

            using var encoded = bitmap.Encode(format, quality);
            return encoded.ToArray();
        }
    }
}
