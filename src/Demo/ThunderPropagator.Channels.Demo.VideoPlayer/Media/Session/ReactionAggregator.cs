using System.Collections.Concurrent;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session
{
    /// <summary>
    /// Validates, rate-limits, and aggregates <c>Video/React</c> submissions (#229's own scope) —
    /// deliberately a standalone type, not a member of <see cref="VideoPlaybackSession"/> itself. That
    /// session's own decode/publish hot path is guarded by <c>_publishGate</c>/its own lifecycle lock;
    /// this type never touches either, which is itself the mechanism that guarantees #229's own AC
    /// ("Reaction work cannot delay frame/audio publication") — reaction bookkeeping simply has no lock
    /// or data structure in common with a single frame/audio packet ever being decoded or published.
    /// </summary>
    /// <remarks>
    /// <b>Expiry</b> is lazy: nothing runs on a timer. Every mutating or reading call prunes whatever
    /// stale timestamps it happens to touch first. A background sweep would need to allocate/lock on its
    /// own schedule regardless of whether anyone is even watching reactions right now — lazy pruning
    /// only ever does work proportional to calls this type is already receiving, and "expired reactions
    /// disappear within the documented tolerance" (#229's own AC) is satisfied exactly: an expired entry
    /// is invisible to the very next <see cref="GetSnapshot"/> call, which is as tight a tolerance as a
    /// polling-based client could ever observe anyway.
    /// <para/>
    /// <b>Two independent timestamp logs</b> are kept per successful reaction — one per reaction type
    /// (for <see cref="GetSnapshot"/>'s own aggregate counts) and one per viewer (for rate-limiting that
    /// viewer's own future submissions) — rather than one shared structure serving both purposes, since
    /// aggregation is keyed by reaction type and rate-limiting is keyed by viewer; conflating them would
    /// make one or the other prune/query pattern awkward for no benefit.
    /// </remarks>
    public sealed class ReactionAggregator
    {
        private readonly IMonotonicClock _clock;
        private readonly IReadOnlySet<string> _allowedReactions;
        private readonly TimeSpan _reactionWindow;
        private readonly int _maxReactionsPerViewerPerWindow;
        private readonly Action<string, string, ReactionRejectionReason>? _onRejected;

        private readonly ConcurrentDictionary<string, ConcurrentQueue<TimeSpan>> _timestampsByReaction = new();
        private readonly ConcurrentDictionary<string, ViewerState> _viewerStates = new();

        /// <param name="clock">Used for every timestamp this instance ever records or prunes by — the same clock instance the owning <see cref="VideoPlaybackSession"/> already uses, so tests can advance both deterministically together.</param>
        /// <param name="allowedReactions">The currently selectable reaction strings. A reaction not in this set is rejected with <see cref="ReactionRejectionReason.Unknown"/> — there is no separate "known but disabled" state; removing a reaction from this set is what disabling it means.</param>
        /// <param name="reactionWindow">How long a recorded reaction remains visible in <see cref="GetSnapshot"/> and counts toward a viewer's own rate limit.</param>
        /// <param name="maxReactionsPerViewerPerWindow">The most reactions one viewer may record within any trailing <paramref name="reactionWindow"/>.</param>
        /// <param name="onRejected">Optional abuse-control hook (#229's own "expose abuse-control hooks" scope) — invoked synchronously, outside any lock this instance holds, whenever <see cref="TryRecord"/> rejects a submission. Mirrors <see cref="DecodedFrameBuffer"/>'s own <c>onFrameDropped</c> callback shape.</param>
        public ReactionAggregator(
            IMonotonicClock clock,
            IReadOnlySet<string> allowedReactions,
            TimeSpan reactionWindow,
            int maxReactionsPerViewerPerWindow,
            Action<string, string, ReactionRejectionReason>? onRejected = null)
        {
            ArgumentNullException.ThrowIfNull(clock);
            ArgumentNullException.ThrowIfNull(allowedReactions);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(reactionWindow, TimeSpan.Zero);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxReactionsPerViewerPerWindow, 0);

            _clock = clock;
            _allowedReactions = allowedReactions;
            _reactionWindow = reactionWindow;
            _maxReactionsPerViewerPerWindow = maxReactionsPerViewerPerWindow;
            _onRejected = onRejected;
        }

        /// <summary>
        /// Attempts to record one <paramref name="reaction"/> for <paramref name="viewerId"/> — validates
        /// the reaction is currently allowed, is not too long, and that this viewer has not exceeded its
        /// own rate limit within the trailing reaction window, in that order. On success, the reaction is
        /// timestamped with this instance's own clock and immediately reflected by <see cref="GetSnapshot"/>.
        /// On rejection, the configured <c>onRejected</c> hook (if any) fires and this call has no effect.
        /// </summary>
        /// <returns><see langword="true"/> if recorded; <see langword="false"/> and the specific <paramref name="reason"/> otherwise.</returns>
        public bool TryRecord(string viewerId, string reaction, out ReactionRejectionReason reason)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(viewerId);
            ArgumentException.ThrowIfNullOrWhiteSpace(reaction);

            if (!_allowedReactions.Contains(reaction))
            {
                reason = ReactionRejectionReason.Unknown;
                _onRejected?.Invoke(viewerId, reaction, reason);
                return false;
            }

            if (reaction.Length > VideoPlayerChannelFeederMessage.ReactionNameMaxLength)
            {
                reason = ReactionRejectionReason.TooLong;
                _onRejected?.Invoke(viewerId, reaction, reason);
                return false;
            }

            var now = _clock.Elapsed;
            var viewerState = _viewerStates.GetOrAdd(viewerId, _ => new ViewerState());
            bool rateLimited;

            lock (viewerState.Lock)
            {
                PruneOlderThan(viewerState.Timestamps, now);

                rateLimited = viewerState.Timestamps.Count >= _maxReactionsPerViewerPerWindow;
                if (!rateLimited)
                    viewerState.Timestamps.Enqueue(now);
            }

            if (rateLimited)
            {
                reason = ReactionRejectionReason.RateLimited;
                _onRejected?.Invoke(viewerId, reaction, reason);
                return false;
            }

            _timestampsByReaction.GetOrAdd(reaction, _ => new ConcurrentQueue<TimeSpan>()).Enqueue(now);

            reason = default;
            return true;
        }

        /// <summary>
        /// The current aggregate counts, pruned of anything older than the configured reaction window as
        /// of this call's own clock reading — one entry per reaction type with at least one still-live
        /// timestamp; a type with none right now is simply omitted, never reported with a zero count.
        /// </summary>
        public IReadOnlyList<VideoReactionCount> GetSnapshot()
        {
            var now = _clock.Elapsed;
            var counts = new List<VideoReactionCount>();

            foreach (var (reaction, timestamps) in _timestampsByReaction)
            {
                var count = PruneOlderThanConcurrent(timestamps, now);
                if (count > 0)
                    counts.Add(new VideoReactionCount(reaction, count));
            }

            return counts;
        }

        /// <summary>Removes every timestamp older than <see cref="_reactionWindow"/> relative to <paramref name="now"/> from the front of <paramref name="timestamps"/> — callers must already hold whatever lock guards this specific queue.</summary>
        private void PruneOlderThan(Queue<TimeSpan> timestamps, TimeSpan now)
        {
            while (timestamps.TryPeek(out var oldest) && now - oldest > _reactionWindow)
                timestamps.Dequeue();
        }

        /// <summary>
        /// Same pruning rule as <see cref="PruneOlderThan(Queue{TimeSpan},TimeSpan)"/>, for the
        /// lock-free <see cref="ConcurrentQueue{T}"/> per-reaction-type logs — a benign race between two
        /// concurrent prune passes on the same queue only ever costs redundant work, never incorrectness
        /// (both converge on the same "everything still within the window" result). Returns the
        /// remaining count after pruning.
        /// </summary>
        private int PruneOlderThanConcurrent(ConcurrentQueue<TimeSpan> timestamps, TimeSpan now)
        {
            while (timestamps.TryPeek(out var oldest) && now - oldest > _reactionWindow)
                timestamps.TryDequeue(out _);

            return timestamps.Count;
        }

        private sealed class ViewerState
        {
            public readonly object Lock = new();
            public readonly Queue<TimeSpan> Timestamps = new();
        }
    }
}
