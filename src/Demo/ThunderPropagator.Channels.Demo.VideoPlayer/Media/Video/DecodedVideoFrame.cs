namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// One raw, not-yet-encoded frame decoded by an <see cref="IVideoFrameSource"/> — distinct from
    /// <see cref="VideoFramePacket"/>, which carries the already-<see cref="VideoFramePacketEncoding"/>-compressed
    /// bytes a later stage (#217) produces from this type.
    /// </summary>
    /// <remarks>
    /// <b>Buffer ownership:</b> the opposite convention from <see cref="VideoFramePacket"/>'s own —
    /// <see cref="Data"/> is <i>not</i> guaranteed to be this instance's private copy. A decoder is free
    /// to hand back a view into its own reused/pooled decode buffer for performance (mirroring how
    /// native decoders commonly reuse one frame buffer across calls). Consequently <see cref="Data"/> is
    /// only valid until <see cref="Dispose"/> is called (which a decoder uses as its signal that the
    /// buffer may be reused for a later frame) — a caller that needs the bytes to outlive this frame's
    /// own lifetime (e.g. across an <c>await</c> that lets the source produce its next frame) must copy
    /// them out first. <see cref="Dispose"/> is safe to call more than once: only the first call actually
    /// releases anything, so "decoded buffers are released exactly once" (#216's own AC) holds regardless
    /// of how many times a caller disposes the same frame.
    /// </remarks>
    public sealed class DecodedVideoFrame : IDisposable
    {
        private Action? _onDispose;

        public DecodedVideoFrame(TimeSpan presentationTimestamp, TimeSpan duration, int width, int height, VideoPixelFormat pixelFormat, ReadOnlyMemory<byte> data, Action? onDispose = null)
        {
            PresentationTimestamp = presentationTimestamp;
            Duration = duration;
            Width = width;
            Height = height;
            PixelFormat = pixelFormat;
            Data = data;
            _onDispose = onDispose;
        }

        /// <summary>This frame's presentation timestamp, exactly as reported by the source — never reconstructed from an assumed frame rate.</summary>
        public TimeSpan PresentationTimestamp { get; }

        /// <summary>How long this frame should remain displayed before the next one, exactly as reported by the source.</summary>
        public TimeSpan Duration { get; }

        /// <summary>Frame width, in pixels.</summary>
        public int Width { get; }

        /// <summary>Frame height, in pixels.</summary>
        public int Height { get; }

        /// <summary>This frame's pixel layout.</summary>
        public VideoPixelFormat PixelFormat { get; }

        /// <summary>The raw pixel buffer. See this type's own remarks on ownership and lifetime.</summary>
        public ReadOnlyMemory<byte> Data { get; }

        /// <summary>
        /// Releases this frame's underlying buffer back to whatever produced it, if anything needs to.
        /// Idempotent — see this type's own remarks.
        /// </summary>
        public void Dispose() => Interlocked.Exchange(ref _onDispose, null)?.Invoke();
    }
}
