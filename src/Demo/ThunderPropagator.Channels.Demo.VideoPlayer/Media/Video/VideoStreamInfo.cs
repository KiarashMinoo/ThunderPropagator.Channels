namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// A video stream's static characteristics, known once <see cref="IVideoFrameSource.OpenAsync"/>
    /// completes. Nothing here is ever used to reconstruct a frame's own timing — every
    /// <see cref="DecodedVideoFrame"/> carries its own <see cref="DecodedVideoFrame.PresentationTimestamp"/>
    /// and <see cref="DecodedVideoFrame.Duration"/> exactly as the source reported them; #216's own AC,
    /// "PTS and duration are not reconstructed from an assumed FPS."
    /// </summary>
    public sealed record VideoStreamInfo
    {
        /// <summary>Frame width, in pixels.</summary>
        public required int Width { get; init; }

        /// <summary>Frame height, in pixels.</summary>
        public required int Height { get; init; }

        /// <summary>The pixel format every <see cref="DecodedVideoFrame"/> this source yields will use.</summary>
        public required VideoPixelFormat PixelFormat { get; init; }

        /// <summary>Whether this source's frame durations vary from frame to frame rather than being constant.</summary>
        public required bool IsVariableFrameRate { get; init; }

        /// <summary>
        /// The source's average/nominal frame rate, in frames per second — purely informational (e.g.
        /// for display in a client UI). Zero if unknown. Never used by this type or
        /// <see cref="IVideoFrameSource"/> itself to compute a frame's timing.
        /// </summary>
        public double NominalFrameRate { get; init; }

        /// <summary>Total stream duration, or <see cref="TimeSpan.Zero"/> for a source of unknown or indeterminate (e.g. live) length.</summary>
        public TimeSpan Duration { get; init; }

        /// <summary>Whether the source has at least one audio stream alongside its video stream. Informational only — this type never yields audio itself; see #224's own scope for audio streaming.</summary>
        public bool HasAudio { get; init; }
    }
}
