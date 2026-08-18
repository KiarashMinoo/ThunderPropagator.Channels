using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// A minimal, test-only stand-in for a consumer-authored feeder (see
    /// AddNotificationsChannelFeeder&lt;TFeeder,...&gt; in NotificationsExtensions). Notifications is
    /// push-only in this repo by design — feeder implementations are provided by package consumers
    /// — so there's nothing in production code to exercise NotificationsFeederConfiguration's
    /// BatchSize, DeduplicationWindow, TimeToLive, and MaxDeliveryAttempts against. This class
    /// demonstrates they're genuinely actionable settings, not inert configuration, by implementing
    /// the batching/dedup/TTL/retry behavior a real feeder would.
    /// </summary>
    internal sealed class SampleNotificationsFeederProcessor(NotificationsFeederConfiguration configuration)
    {
        private readonly List<(string Key, DateTime SeenAt)> _recentlySeen = [];

        /// <summary>
        /// Deduplicates by (UserId, Id) within DeduplicationWindow, drops expired messages per
        /// TimeToLive, then groups whatever remains into batches of at most BatchSize.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<NotificationsChannelFeederMessage>> Batch(
            IEnumerable<NotificationsChannelFeederMessage> incoming, DateTime now)
        {
            var batches = new List<List<NotificationsChannelFeederMessage>>();

            foreach (var message in Deduplicate(incoming, now))
            {
                if (IsExpired(message, now))
                    continue;

                if (batches.Count == 0 || batches[^1].Count >= configuration.BatchSize)
                    batches.Add([]);

                batches[^1].Add(message);
            }

            return batches;
        }

        private IEnumerable<NotificationsChannelFeederMessage> Deduplicate(IEnumerable<NotificationsChannelFeederMessage> incoming, DateTime now)
        {
            _recentlySeen.RemoveAll(seen => now - seen.SeenAt > configuration.DeduplicationWindow);

            foreach (var message in incoming)
            {
                var key = $"{message.UserId}:{message.Id}";

                if (configuration.DeduplicationWindow > TimeSpan.Zero && _recentlySeen.Any(seen => seen.Key == key))
                    continue;

                _recentlySeen.Add((key, now));
                yield return message;
            }
        }

        private bool IsExpired(NotificationsChannelFeederMessage message, DateTime now)
            => configuration.TimeToLive is { } ttl && now - message.Date > ttl;

        /// <summary>
        /// Retries attemptDelivery (1-indexed attempt number) up to MaxDeliveryAttempts times,
        /// stopping as soon as it returns true. Returns false if every attempt is exhausted.
        /// </summary>
        public bool TryDeliver(Func<int, bool> attemptDelivery)
        {
            for (var attempt = 1; attempt <= configuration.MaxDeliveryAttempts; attempt++)
            {
                if (attemptDelivery(attempt))
                    return true;
            }

            return false;
        }
    }
}
