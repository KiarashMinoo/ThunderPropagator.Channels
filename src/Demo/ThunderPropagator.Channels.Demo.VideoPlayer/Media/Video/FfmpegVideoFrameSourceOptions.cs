namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// Configuration for <see cref="FfmpegVideoFrameSource"/>. See that type's own remarks for the
    /// native FFmpeg shared libraries <see cref="RootPath"/> must point at.
    /// </summary>
    public sealed class FfmpegVideoFrameSourceOptions
    {
        /// <summary>
        /// Directory containing the native FFmpeg shared libraries this platform needs
        /// (<c>avformat</c>, <c>avcodec</c>, <c>avutil</c>, <c>swscale</c> — as <c>.dll</c> on Windows,
        /// <c>.so</c> on Linux, <c>.dylib</c> on macOS). Left <see langword="null"/>, the default OS
        /// library search path is used instead (e.g. a system-wide FFmpeg install already on <c>PATH</c>/
        /// <c>LD_LIBRARY_PATH</c>). See <see cref="FfmpegVideoFrameSource"/>'s own remarks for this
        /// package's native deployment requirements.
        /// </summary>
        public string? RootPath { get; set; }

        /// <summary>Maximum output frame width. A source wider than this is scaled down — never up — preserving its aspect ratio. Default: 1280.</summary>
        public int MaxWidth { get; set; } = 1280;

        /// <summary>Maximum output frame height. Default: 720.</summary>
        public int MaxHeight { get; set; } = 720;
    }
}
