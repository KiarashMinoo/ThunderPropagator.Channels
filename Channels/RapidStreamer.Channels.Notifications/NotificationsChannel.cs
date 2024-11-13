using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RapidStreamer.Application.Channels;
using RapidStreamer.Application.Channels.Snapshots;
using RapidStreamer.Application.Channels.Subscribers;
using RapidStreamer.BuildingBlocks.Application;
using RapidStreamer.BuildingBlocks.Application.Enums;

namespace RapidStreamer.Channels.Notifications
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

            SearchSnapshotsAsync(snapshotEntries => snapshotEntries
                        .Where(snapshotEntry => snapshotEntry.CastType == CastType.Broadcast)
                        .GroupBy(snapshotEntry => snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.Id)])
                        .Where(grouped => grouped.All(snapshotEntry => snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.UserId)]!.ToString() != userId))
                        .Select(grouped => grouped.First()),
                    0,
                    0,
                    _cancellationToken
                )
                .Result
                .ToList()
                .ForEach(snapshotEntry => base.EmitMessage(null, CastType.Broadcast, snapshotEntry.Snapshot, typeof(NotificationsChannelFeederMessage)));
        }

        protected override void EmitMessage(FeederMessage feederMessage)
        {
            var notificationsChannelFeederMessage = (NotificationsChannelFeederMessage)feederMessage;
            if (string.IsNullOrWhiteSpace(notificationsChannelFeederMessage.UserId))
            {
                SearchSnapshotsAsync(snapshotEntries => snapshotEntries
                            .Where(snapshotEntry => snapshotEntry.CastType == CastType.Broadcast)
                            .GroupBy(snapshotEntry => snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.UserId)])
                            .Select(grouped => grouped.First()),
                        0,
                        0,
                        _cancellationToken
                    )
                    .Result
                    .ToList()
                    .ForEach(snapshotEntry =>
                    {
                        var userId = snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.UserId)];
                        notificationsChannelFeederMessage.UserId = userId!.ToString();
                        notificationsChannelFeederMessage["HashKey"] = null;

                        base.EmitMessage(notificationsChannelFeederMessage);
                    });
            }
            else
                base.EmitMessage(notificationsChannelFeederMessage);
        }

        public override Task<SnapshotEntry[]> SnapshotsToSendAsync(Subscription subscription, IEnumerable<int> hashKeys, CancellationToken cancellationToken = default)
            => SearchSnapshotsAsync(snapshotEntry => subscription.SubscribedPrograms.SubscribedKeys.IsEquals(snapshotEntry.Snapshot) && !hashKeys.Contains(snapshotEntry.HashKey),
                0,
                0,
                cancellationToken);
    }
}