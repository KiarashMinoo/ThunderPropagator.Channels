namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Sits between an audio decoder (<see cref="IAudioFrameSource.ReadFramesAsync"/>) and whatever
    /// paces and publishes audio (a session's own publish loop) — the audio-side counterpart to
    /// <see cref="DecodedFrameBuffer"/>, sharing its own strictly-bounded, single-producer/single-consumer
    /// design and "select the newest frame due at or before the current position, disposing superseded
    /// older ones" behavior verbatim. See that type's own remarks — every word of them applies here with
    /// <see cref="DecodedAudioFrame"/> in place of <c>DecodedVideoFrame</c>. Kept as a separate,
    /// non-generic type rather than a shared/generic one so #219's already-shipped, already-tested
    /// <see cref="DecodedFrameBuffer"/> never has to change to accommodate this ticket's own needs.
    /// </summary>
    public sealed class DecodedAudioBuffer : IDisposable
    {
#if NET9_0_OR_GREATER
        private readonly Lock _lock = new();
#else
        private readonly object _lock = new();
#endif
        private readonly Queue<DecodedAudioFrame> _frames = new();
        private readonly Action<FrameDropReason>? _onFrameDropped;
        private bool _disposed;

        /// <param name="capacity">Maximum number of frames held at once. Must be strictly positive.</param>
        /// <param name="onFrameDropped">Optional callback invoked (synchronously, under this instance's own lock) whenever a frame is dropped, and why.</param>
        public DecodedAudioBuffer(int capacity, Action<FrameDropReason>? onFrameDropped = null)
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

        /// <summary>Adds <paramref name="frame"/> to the buffer, evicting and disposing the oldest one first if already at <see cref="Capacity"/>. Never blocks or throws for being full.</summary>
        public void Enqueue(DecodedAudioFrame frame)
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

        /// <summary>Returns the newest buffered frame at or before <paramref name="currentMediaTime"/>, disposing every older, now-superseded frame found along the way. See <see cref="DecodedFrameBuffer.TryTakeCurrent"/>'s own remarks.</summary>
        public bool TryTakeCurrent(TimeSpan currentMediaTime, out DecodedAudioFrame? frame)
        {
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                DecodedAudioFrame? selected = null;

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
