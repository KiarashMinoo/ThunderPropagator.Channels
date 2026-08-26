using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// One subscriber's own small, strictly bounded outgoing queue — the publish-side half of #219's
    /// scope, "Prevent one slow subscriber from blocking the session publisher or other viewers." A
    /// session publisher holding one independent instance per subscriber and calling <see cref="Enqueue"/>
    /// on each in turn can never have a slow subscriber's full queue stall delivery to any other
    /// subscriber — each instance owns its own state and lock, entirely independent of every other one.
    /// </summary>
    /// <typeparam name="T">
    /// The payload type queued for delivery — deliberately unconstrained (e.g. an already-encoded
    /// <c>VideoFramePacket</c>), since this type has no opinion on what a session publisher actually
    /// sends. If <typeparamref name="T"/> owns a resource that must be released (as
    /// <see cref="DecodedVideoFrame"/> does), pass the constructor's own <c>onItemDropped</c> callback
    /// to release it — this type never assumes disposability itself.
    /// </typeparam>
    public sealed class SubscriberFrameQueue<T> : IDisposable
    {
#if NET9_0_OR_GREATER
        private readonly Lock _lock = new();
#else
        private readonly object _lock = new();
#endif
        private readonly Queue<T> _items = new();
        private readonly Action<T>? _onItemDropped;
        private readonly Action<FrameDropReason>? _onFrameDropped;
        private bool _disposed;

        /// <param name="capacity">Maximum number of items held at once for this one subscriber. Must be strictly positive.</param>
        /// <param name="onItemDropped">Optional callback invoked with an item evicted for capacity, or discarded on <see cref="Dispose"/> — e.g. to dispose it if <typeparamref name="T"/> owns a resource.</param>
        /// <param name="onFrameDropped">Optional callback invoked whenever an item is dropped for capacity, and why.</param>
        public SubscriberFrameQueue(int capacity, Action<T>? onItemDropped = null, Action<FrameDropReason>? onFrameDropped = null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(capacity, 0);

            Capacity = capacity;
            _onItemDropped = onItemDropped;
            _onFrameDropped = onFrameDropped;
        }

        /// <summary>The bound passed to the constructor.</summary>
        public int Capacity { get; }

        /// <summary>Number of items currently queued for this subscriber.</summary>
        public int Count
        {
            get
            {
                lock (_lock)
                    return _items.Count;
            }
        }

        /// <summary>
        /// Adds <paramref name="item"/> to this subscriber's own queue. If already at
        /// <see cref="Capacity"/>, the oldest queued item is evicted first
        /// (<see cref="FrameDropReason.SubscriberQueueCapacityExceeded"/>) — this call never blocks or
        /// throws for being full, and never touches any other <see cref="SubscriberFrameQueue{T}"/>
        /// instance's own state.
        /// </summary>
        public void Enqueue(T item)
        {
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (_items.Count >= Capacity)
                {
                    var dropped = _items.Dequeue();
                    _onItemDropped?.Invoke(dropped);
                    _onFrameDropped?.Invoke(FrameDropReason.SubscriberQueueCapacityExceeded);
                }

                _items.Enqueue(item);
            }
        }

        /// <summary>Removes and returns the oldest queued item, if any.</summary>
        public bool TryDequeue(out T? item)
        {
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _items.TryDequeue(out item);
            }
        }

        /// <summary>Discards every currently queued item, running <c>onItemDropped</c> for each. Safe to call more than once.</summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;

                while (_items.Count > 0)
                {
                    var item = _items.Dequeue();
                    _onItemDropped?.Invoke(item);
                }
            }
        }
    }
}
