namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// Pure aspect-ratio-preserving scaling math shared by any decoder that needs to fit a source
    /// frame within a configured bound (e.g. <see cref="FfmpegVideoFrameSource"/>'s own
    /// <see cref="FfmpegVideoFrameSourceOptions.MaxWidth"/>/<see cref="FfmpegVideoFrameSourceOptions.MaxHeight"/>)
    /// — #217's own scope: "Scale to configured target bounds while preserving aspect ratio." Kept
    /// free of any native dependency so it is testable on its own.
    /// </summary>
    public static class VideoFrameScaling
    {
        /// <summary>
        /// Computes the largest size that fits within <paramref name="maxWidth"/> x
        /// <paramref name="maxHeight"/> while preserving <paramref name="sourceWidth"/>/<paramref name="sourceHeight"/>'s
        /// own aspect ratio, without ever upscaling past the source's own size. The result is always
        /// even in both dimensions (rounded down), since most pixel formats and encoders — including
        /// 4:2:0 chroma-subsampled YUV — require it.
        /// </summary>
        public static (int Width, int Height) ComputeScaledSize(int sourceWidth, int sourceHeight, int maxWidth, int maxHeight)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sourceWidth, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sourceHeight, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxWidth, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxHeight, 0);

            var scale = Math.Min(1.0, Math.Min((double)maxWidth / sourceWidth, (double)maxHeight / sourceHeight));

            var width = RoundDownToEven(Math.Max(2, (int)Math.Round(sourceWidth * scale)));
            var height = RoundDownToEven(Math.Max(2, (int)Math.Round(sourceHeight * scale)));

            return (width, height);
        }

        private static int RoundDownToEven(int value) => value - value % 2;
    }
}
