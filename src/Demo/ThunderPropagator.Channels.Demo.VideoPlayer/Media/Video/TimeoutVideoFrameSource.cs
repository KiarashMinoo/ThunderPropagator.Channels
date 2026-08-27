namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// Wraps another <see cref="IVideoFrameSource"/>, bounding <see cref="OpenAsync"/> to this instance's
    /// own configured timeout — #238's own scope, actually applying
    /// <see cref="Configuration.VideoPlayerChannelConfiguration.SourceOpenTimeout"/> as a real
    /// <see cref="CancellationTokenSource"/> timeout, which that property's own remarks say nothing in
    /// this codebase did before this ticket. Every other member delegates straight through to the wrapped
    /// source unchanged — this type has no opinion on anything except how long <see cref="OpenAsync"/>
    /// may take.
    /// </summary>
    /// <remarks>
    /// Correctness here depends on the wrapped source's own <see cref="OpenAsync"/> actually observing
    /// the <see cref="CancellationToken"/> this type passes it — true for
    /// <see cref="FfmpegVideoFrameSource"/>, whose own <c>Open</c> wires FFmpeg's AVIOInterruptCB
    /// mechanism to exactly that token (see its own remarks), so a native <c>avformat_open_input</c>
    /// blocked on a slow/unreachable source is actually interrupted, not merely raced against
    /// client-side. A hand-written <see cref="IVideoFrameSource"/> that ignores its own cancellation
    /// token parameter would simply run to whatever completion it was always going to reach — this type
    /// cannot force cooperation it isn't given.
    /// </remarks>
    public sealed class TimeoutVideoFrameSource : IVideoFrameSource
    {
        private readonly IVideoFrameSource _inner;
        private readonly TimeSpan _timeout;

        /// <param name="inner">The real source to delegate every call to.</param>
        /// <param name="timeout">How long <see cref="OpenAsync"/> may take before it is treated as failed. Must be strictly positive.</param>
        public TimeoutVideoFrameSource(IVideoFrameSource inner, TimeSpan timeout)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

            _inner = inner;
            _timeout = timeout;
        }

        /// <inheritdoc/>
        public VideoStreamInfo? StreamInfo => _inner.StreamInfo;

        /// <inheritdoc/>
        /// <exception cref="VideoFrameSourceException">
        /// This instance's own configured timeout elapsed before the wrapped source's own
        /// <see cref="IVideoFrameSource.OpenAsync"/> completed. A genuine cancellation via the caller's
        /// own <paramref name="cancellationToken"/> still surfaces as <see cref="OperationCanceledException"/>,
        /// never masked as this — see this type's own remarks.
        /// </exception>
        public async Task<VideoStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default)
        {
            using var timeoutCancellation = new CancellationTokenSource(_timeout);
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);

            try
            {
                return await _inner.OpenAsync(source, linkedCancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new VideoFrameSourceException($"Opening the video source timed out after {_timeout}.");
            }
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<DecodedVideoFrame> ReadFramesAsync(TimeSpan startPosition, CancellationToken cancellationToken = default) =>
            _inner.ReadFramesAsync(startPosition, cancellationToken);

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }
}
