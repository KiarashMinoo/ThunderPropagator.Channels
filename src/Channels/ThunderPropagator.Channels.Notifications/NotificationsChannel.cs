using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Snapshots;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.BuildingBlocks.Application.Enums;

namespace ThunderPropagator.Channels.Notifications
{
    public
#if !DEBUG
        sealed
#endif
        class NotificationsChannel<TNotificationsChannelConfiguration> : AbstractChannel<NotificationsChannelMetadata<TNotificationsChannelConfiguration>, TNotificationsChannelConfiguration>
        where TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()
    {
        private readonly CancellationToken _cancellationToken;

        public NotificationsChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _cancellationToken = serviceProvider.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        }

        protected override void OnSubscriptionAdded(Subscription subscription)
        {
            base.OnSubscriptionAdded(subscription);

            var userId = subscription.SubscribedPrograms.SubscribedKeys[nameof(NotificationsChannelFeederMessage.UserId)];

            // AbstractChannel.Subscribe (which invokes this hook) is a synchronous, non-awaitable
            // API with no asynchronous subscription hook available yet, so this can't await without
            // blocking the caller. Fire-and-forget mirrors the base class's own EmitMessage pattern
            // for the same reason; errors are caught and logged so a fault here doesn't crash on GC.
            _ = SendMissedBroadcastsAsync(userId, _cancellationToken);
        }

        private async Task SendMissedBroadcastsAsync(string userId, CancellationToken cancellationToken)
        {
            try
            {
                var snapshotEntries = await SearchSnapshotsAsync(snapshotEntries => snapshotEntries
                            .Where(snapshotEntry => snapshotEntry.CastType == CastType.Broadcast)
                            .GroupBy(snapshotEntry => snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.Id)])
                            .Where(grouped => grouped.All(snapshotEntry => snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.UserId)]!.ToString() != userId))
                            .Select(grouped => grouped.First()),
                        0,
                        0,
                        cancellationToken)
                    .ConfigureAwait(false);

                foreach (var snapshotEntry in snapshotEntries)
                {
                    var hashKey = HashCode.Combine(
                        snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.UserId)],
                        snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.Id)]);

                    await EmitMessageAsync(hashKey, CastType.Broadcast, snapshotEntry.Snapshot, typeof(NotificationsChannelFeederMessage), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to send missed broadcasts to user {UserId} on channel {ChannelName}.", userId, Metadata.ChannelName);
            }
        }

        protected override void EmitMessage(FeederMessage feederMessage)
        {
            var emitTask = EmitMessageAsync(feederMessage, _cancellationToken);

            if (emitTask.IsCompletedSuccessfully)
                return;

            _ = emitTask.ContinueWith(
                task => Logger.LogError(task.Exception, "Failed to emit message on channel {ChannelName}.", Metadata.ChannelName),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        protected override async Task EmitMessageAsync(FeederMessage feederMessage, CancellationToken cancellationToken = default)
        {
            var notificationsChannelFeederMessage = (NotificationsChannelFeederMessage)feederMessage;
            if (string.IsNullOrWhiteSpace(notificationsChannelFeederMessage.UserId))
            {
                var snapshotEntries = await SearchSnapshotsAsync(snapshotEntries => snapshotEntries
                            .Where(snapshotEntry => snapshotEntry.CastType == CastType.Broadcast)
                            .GroupBy(snapshotEntry => snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.UserId)])
                            .Select(grouped => grouped.First()),
                        0,
                        0,
                        cancellationToken)
                    .ConfigureAwait(false);

                foreach (var snapshotEntry in snapshotEntries)
                {
                    // A fresh copy per recipient — base.EmitMessageAsync can queue or otherwise hold
                    // onto the reference beyond this call, so retargeting the same shared instance's
                    // UserId for the next recipient would let every recipient observe whichever
                    // UserId happened to be set last (or race on it entirely under concurrent
                    // delivery). The original notificationsChannelFeederMessage is never touched.
                    var userId = snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.UserId)];
                    var recipientMessage = new NotificationsChannelFeederMessage(notificationsChannelFeederMessage) { UserId = userId!.ToString() };
                    AssignHashKey(recipientMessage);

                    await base.EmitMessageAsync(recipientMessage, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                AssignHashKey(notificationsChannelFeederMessage);
                await base.EmitMessageAsync(notificationsChannelFeederMessage, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// UserId is now the only subscribing key (#61), so the framework's default snapshot hash
        /// (derived from subscribing keys) would collapse every notification for a user into a
        /// single overwritten entry. A notification is a distinct event, not a value that
        /// supersedes the last one, so the hash is instead derived from (UserId, Id) — stable
        /// across re-emission (e.g. the missed-broadcast replay path) and independent of the
        /// live-routing key.
        /// </summary>
        private static void AssignHashKey(NotificationsChannelFeederMessage message)
            => message.Envelope.HashKey = HashCode.Combine(message.UserId, message.Id);

        public override Task<SnapshotEntry[]> SnapshotsToSendAsync(Subscription subscription, CancellationToken cancellationToken = default)
            => SearchSnapshotsAsync(snapshotEntry => subscription.SubscribedPrograms.SubscribedKeys.IsEquals(snapshotEntry.Snapshot),
                0,
                0,
                cancellationToken);

        /// <summary>
        /// Looks up a recipient's stored notifications, optionally narrowed to a date range.
        /// UserId is the only live subscription key (see #61); this is a separate, explicit query
        /// path for historical retrieval and has no effect on subscription identity or routing.
        /// Leaving <paramref name="dateRange"/> null returns the recipient's full history. Entries
        /// with no recorded Date are excluded whenever a range is supplied, since there's nothing to
        /// compare against.
        /// </summary>
        public Task<SnapshotEntry[]> SearchHistoricalNotificationsAsync(
            string userId,
            NotificationsHistoricalDateRangeFilter? dateRange = null,
            CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(userId);

            return SearchSnapshotsAsync(snapshotEntry =>
                    userId.Equals(snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.UserId)]?.ToString()) &&
                    (dateRange is null ||
                     (snapshotEntry.Snapshot.TryGetValue(nameof(NotificationsChannelFeederMessage.Date), out var date) &&
                      date is DateTime dateTime &&
                      dateRange.IsSatisfiedBy(dateTime))),
                0,
                0,
                cancellationToken);
        }
    }
}
