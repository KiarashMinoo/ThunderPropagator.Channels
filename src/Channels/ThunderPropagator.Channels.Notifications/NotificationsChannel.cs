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
                    await EmitMessageAsync(null, CastType.Broadcast, snapshotEntry.Snapshot, typeof(NotificationsChannelFeederMessage), cancellationToken).ConfigureAwait(false);
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
                    var userId = snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.UserId)];
                    notificationsChannelFeederMessage.UserId = userId!.ToString();
                    notificationsChannelFeederMessage = notificationsChannelFeederMessage.ResetHashKey();

                    await base.EmitMessageAsync(notificationsChannelFeederMessage, cancellationToken).ConfigureAwait(false);
                }
            }
            else
                await base.EmitMessageAsync(notificationsChannelFeederMessage, cancellationToken).ConfigureAwait(false);
        }

        public override Task<SnapshotEntry[]> SnapshotsToSendAsync(Subscription subscription, CancellationToken cancellationToken = new CancellationToken())
            => SearchSnapshotsAsync(snapshotEntry => subscription.SubscribedPrograms.SubscribedKeys.IsEquals(snapshotEntry.Snapshot),
                0,
                0,
                cancellationToken);
    }
}
