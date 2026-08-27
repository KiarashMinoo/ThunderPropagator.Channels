using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio
{
    /// <summary>
    /// The audio-side counterpart to <see cref="TimeoutVideoFrameSource"/> — see that type's own remarks
    /// in full; every word applies here with <see cref="IAudioFrameSource"/>/<see cref="AudioStreamInfo"/>
    /// in place of the video equivalents. Kept as a separate, non-generic type rather than a shared
    /// wrapper for the same reason <see cref="DecodedAudioBuffer"/> is its own type rather than reusing
    /// <see cref="DecodedFrameBuffer"/>: the two source interfaces share no common base beyond
    /// <see cref="IAsyncDisposable"/>.
    /// </summary>
    public sealed class TimeoutAudioFrameSource : IAudioFrameSource
    {
        private readonly IAudioFrameSource _inner;
        private readonly TimeSpan _timeout;

        /// <param name="inner">The real source to delegate every call to.</param>
        /// <param name="timeout">How long <see cref="OpenAsync"/> may take before it is treated as failed. Must be strictly positive.</param>
        public TimeoutAudioFrameSource(IAudioFrameSource inner, TimeSpan timeout)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

            _inner = inner;
            _timeout = timeout;
        }

        /// <inheritdoc/>
        public AudioStreamInfo? StreamInfo => _inner.StreamInfo;

        /// <inheritdoc/>
        /// <exception cref="VideoFrameSourceException">
        /// This instance's own configured timeout elapsed before the wrapped source's own
        /// <see cref="IAudioFrameSource.OpenAsync"/> completed. A genuine cancellation via the caller's
        /// own <paramref name="cancellationToken"/> still surfaces as <see cref="OperationCanceledException"/>,
        /// never masked as this — see <see cref="TimeoutVideoFrameSource"/>'s own remarks.
        /// </exception>
        public async Task<AudioStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default)
        {
            using var timeoutCancellation = new CancellationTokenSource(_timeout);
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);

            try
            {
                return await _inner.OpenAsync(source, linkedCancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new VideoFrameSourceException($"Opening the audio source timed out after {_timeout}.");
            }
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<DecodedAudioFrame> ReadFramesAsync(TimeSpan startPosition, CancellationToken cancellationToken = default) =>
            _inner.ReadFramesAsync(startPosition, cancellationToken);

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }
}
