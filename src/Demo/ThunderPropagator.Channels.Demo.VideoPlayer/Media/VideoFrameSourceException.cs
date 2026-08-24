namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Thrown by an <see cref="IVideoFrameSource"/> implementation when opening or reading fails for a
    /// reason intrinsic to the source itself (an unreadable/corrupt stream, a decoder failure, an
    /// unreachable source) — distinct from <see cref="OperationCanceledException"/> (caller-requested
    /// cancellation) and from an <see cref="ArgumentException"/>/<see cref="InvalidOperationException"/>
    /// a caller's own misuse of the interface would raise.
    /// </summary>
    public sealed class VideoFrameSourceException : Exception
    {
        public VideoFrameSourceException(string message) : base(message)
        {
        }

        public VideoFrameSourceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
