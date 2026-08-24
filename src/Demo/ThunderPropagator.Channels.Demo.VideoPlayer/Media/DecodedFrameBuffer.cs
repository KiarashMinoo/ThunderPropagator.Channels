namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Sits between a decoder (<see cref="IVideoFrameSource.ReadFramesAsync"/>) and whatever paces and
    /// publishes frames (<see cref="FramePacer"/>) — a small, strictly bounded queue so a decoder that
    /// outpaces publication cannot grow memory without limit, and so playback catches up to the current
    /// media position instead of draining an ever-growing backlog frame by frame. #219's own scope in
    /// full, for the decode side (see <see cref="SubscriberFrameQueue{T}"/> for the publish/subscriber
    /// side).
    /// </summary>
    /// <remarks>
    /// Single-producer (the decoder calling <see cref="Enqueue"/>), single-consumer (the publisher
    /// calling <see cref="TryTakeCurrent"/>) — a second concurrent consumer would race over which frame
    /// each one observes as "current," which no caller in this codebase needs. Frames must already
    /// arrive in non-decreasing <see cref="DecodedVideoFrame.PresentationTimestamp"/> order (guaranteed
    /// by <see cref="IVideoFrameSource"/>'s own contract) — this type does not sort them.
    /// </remarks>
    public sealed class DecodedFrameBuffer : IDisposable
    {
#if NET9_0_OR_GREATER
        private readonly Lock _lock = new();
#else
        private readonly object _lock = new();
#endif
        private readonly Queue<DecodedVideoFrame> _frames = new();
        private readonly Action<FrameDropReason>? _onFrameDropped;
        private bool _disposed;

        /// <param name="capacity">Maximum number of frames held at once. Must be strictly positive.</param>
        /// <param name="onFrameDropped">Optional callback invoked (synchronously, under this instance's own lock) whenever a frame is dropped, and why.</param>
        public DecodedFrameBuffer(int capacity, Action<FrameDropReason>? onFrameDropped = null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(capacity, 0);

            Capacity = capacity;
            _onFrameDropped = onFrameDropped;
        }

        /// <summary>The bound passed to the constructor.</summary>
        public int Capacity { get; }

        /// <summary>Number of frames currently buffered.</summary>
        public int Count
        {
            get
            {
                lock (_lock)
                    return _frames.Count;
            }
        }

        /// <summary>
        /// Adds <paramref name="frame"/> to the buffer. If already at <see cref="Capacity"/>, the oldest
        /// buffered frame is evicted and disposed first (<see cref="FrameDropReason.DecodeBufferCapacityExceeded"/>)
        /// — this call never blocks or throws for being full; it always accepts <paramref name="frame"/>.
        /// </summary>
        public void Enqueue(DecodedVideoFrame frame)
        {
            ArgumentNullException.ThrowIfNull(frame);

            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (_frames.Count >= Capacity)
                {
                    _frames.Dequeue().Dispose();
                    _onFrameDropped?.Invoke(FrameDropReason.DecodeBufferCapacityExceeded);
                }

                _frames.Enqueue(frame);
            }
        }

        /// <summary>
        /// Returns the newest buffered frame whose <see cref="DecodedVideoFrame.PresentationTimestamp"/>
        /// is at or before <paramref name="currentMediaTime"/> — the one that's actually due right now —
        /// disposing every older, now-superseded frame found along the way
        /// (<see cref="FrameDropReason.SupersededByNewerFrame"/>). Frames still in the future relative to
        /// <paramref name="currentMediaTime"/> are left untouched for a later call. Returns
        /// <see langword="false"/> (with <paramref name="frame"/> left <see langword="null"/>) if nothing
        /// buffered is due yet.
        /// </summary>
        public bool TryTakeCurrent(TimeSpan currentMediaTime, out DecodedVideoFrame? frame)
        {
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                DecodedVideoFrame? selected = null;

                while (_frames.Count > 0 && _frames.Peek().PresentationTimestamp <= currentMediaTime)
                {
                    var candidate = _frames.Dequeue();

                    if (selected is not null)
                    {
                        selected.Dispose();
                        _onFrameDropped?.Invoke(FrameDropReason.SupersededByNewerFrame);
                    }

                    selected = candidate;
                }

                frame = selected;
                return selected is not null;
            }
        }

        /// <summary>Disposes every currently buffered frame. Safe to call more than once.</summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;

                while (_frames.Count > 0)
                    _frames.Dequeue().Dispose();
            }
        }
    }
}
