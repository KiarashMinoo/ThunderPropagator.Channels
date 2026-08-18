using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Exceptions;
using ThunderPropagator.Application.Channels.Snapshots;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.BuildingBlocks.Application.Enums;

namespace ThunderPropagator.Channels.Notifications
{
    /// <summary>
    /// Push-only notification channel: messages are emitted directly via
    /// <see cref="ThunderPropagator.Application.Channels.IChannel.EmitMessage"/>/<c>EmitMessageAsync</c>
    /// rather than pulled by a feeder this package provides — see
    /// <see cref="NotificationsFeederConfiguration"/> for the settings a consumer-authored feeder is
    /// expected to honor. A message with <see cref="NotificationsChannelFeederMessage.UserId"/> set
    /// is delivered to that recipient; left unset, it's broadcast to every current subscriber, and a
    /// subscriber who joins afterward is caught up on broadcasts it missed. See
    /// <see cref="SearchHistoricalNotificationsAsync"/> for querying a recipient's stored history
    /// independently of live subscription.
    /// </summary>
    /// <remarks>
    /// <para><b>ChannelConfiguration.IsEnabled contract (see #72):</b> checked fresh on every call,
    /// never cached — toggling it takes effect on the very next operation, with no restart or
    /// reconnect required.</para>
    /// <para>While disabled: <see cref="ThunderPropagator.Application.Channels.IChannel.EmitMessage"/>/<c>EmitMessageAsync</c>
    /// throws <see cref="ThunderPropagator.Application.Channels.Exceptions.ChannelIsNotEnabledException"/>
    /// for every new publish — broadcast and targeted alike — and logs a warning naming the channel;
    /// <c>Subscribe</c> throws the same exception, so no new subscription can be created;
    /// <c>SnapshotsToSendAsync</c> and <see cref="SearchHistoricalNotificationsAsync"/> throw it too,
    /// so snapshot/history queries are rejected rather than returning stale or partial data.</para>
    /// <para>What's <i>not</i> affected: a subscription created before the channel was disabled stays
    /// registered (it's simply unable to receive new snapshot replay or historical query results
    /// until re-enabled), and snapshot entries already stored are retained, not cleared, while
    /// disabled.</para>
    /// <para>What's out of scope for this contract: "queued" or "in-flight" notification state.
    /// This channel is push-only and holds no internal delivery queue of its own — emitting a message
    /// either completes synchronously against current subscriptions/snapshot storage or throws
    /// immediately; there's no in-process backlog to pause, drain, or acknowledge. A consumer's own
    /// feeder implementation (see <see cref="NotificationsFeederConfiguration"/>) is responsible for
    /// whatever it queues before calling into this channel, and for deciding how its own queue reacts
    /// to <c>IsEnabled</c> — this contract only covers the channel's own boundary.</para>
    /// </remarks>
    public
#if !DEBUG
        sealed
#endif
        class NotificationsChannel<TNotificationsChannelConfiguration> : AbstractChannel<NotificationsChannelMetadata<TNotificationsChannelConfiguration>, TNotificationsChannelConfiguration>
        where TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()
    {
        private readonly CancellationToken _cancellationToken;

        /// <summary>Resolves the shared application-stopping token used to cancel background/fire-and-forget work started by this channel.</summary>
        public NotificationsChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _cancellationToken = serviceProvider.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        }

        /// <summary>
        /// Catches up a newly added subscription on any broadcast it missed while it wasn't
        /// subscribed yet, by re-emitting one representative copy of each such broadcast. Runs
        /// fire-and-forget (the base subscription hook is synchronous); failures are logged rather
        /// than thrown.
        /// </summary>
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

        /// <summary>
        /// Synchronous fire-and-forget entry point: starts <see cref="EmitMessageAsync"/> and, if it
        /// doesn't complete synchronously, logs a failure rather than propagating the exception back
        /// to this synchronous call.
        /// </summary>
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

        /// <summary>
        /// Routes <paramref name="feederMessage"/> based on
        /// <see cref="NotificationsChannelFeederMessage.UserId"/>: with UserId set, emits it directly
        /// to that recipient; left unset, fans it out as a broadcast — a fresh per-recipient copy for
        /// each subscriber known via stored snapshots, so <paramref name="feederMessage"/> itself is
        /// never mutated and no two recipients share the same emitted instance. Throws
        /// <see cref="NotificationsChannelFeederMessageValidationException"/> before doing anything
        /// else if <see cref="NotificationsChannelFeederMessage.Id"/> or
        /// <see cref="NotificationsChannelFeederMessage.Subject"/> was never set — this is the
        /// earliest point a message built via the public parameterless constructor and object
        /// initializer can reliably be checked, since an unset property never invokes its own
        /// validating setter (see #68).
        /// </summary>
        /// <remarks>
        /// <b>Disabled-channel contract (see #72):</b> throws <see cref="ChannelIsNotEnabledException"/>
        /// immediately when <c>ChannelConfiguration.IsEnabled</c> is false, for both the broadcast and
        /// the targeted path alike — new publishes are rejected outright rather than silently dropped,
        /// consistent with <c>Subscribe</c>, <c>SnapshotsToSendAsync</c>, and
        /// <see cref="SearchHistoricalNotificationsAsync"/>, which already throw the same way. This
        /// channel has no internal queue or in-flight delivery state of its own to pause or drain —
        /// it's push-only, and a consumer's own feeder implementation (see
        /// <see cref="NotificationsFeederConfiguration"/>) is responsible for anything it queues before
        /// calling <c>EmitMessage</c>/<c>EmitMessageAsync</c>. <c>IsEnabled</c> is read fresh on every
        /// call rather than cached, so toggling it at runtime takes effect immediately on the next
        /// publish or subscription attempt; no restart is required. Existing subscriptions and stored
        /// snapshots are untouched by disabling the channel — only new publishes, new subscriptions,
        /// and snapshot queries are rejected while disabled.
        /// </remarks>
        protected override async Task EmitMessageAsync(FeederMessage feederMessage, CancellationToken cancellationToken = default)
        {
            if (!ChannelConfiguration.IsEnabled)
            {
                Logger.LogWarning("Rejected message emission on channel {ChannelName} because the channel is disabled.", Metadata.ChannelName);
                throw new ChannelIsNotEnabledException();
            }

            var notificationsChannelFeederMessage = (NotificationsChannelFeederMessage)feederMessage;
            notificationsChannelFeederMessage.ValidateRequiredFields();

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

        /// <summary>
        /// Stored snapshot entries matching <paramref name="subscription"/>'s subscribed keys —
        /// i.e. the given recipient's own notifications — used to replay state to a subscription
        /// when it's established. See <see cref="SearchHistoricalNotificationsAsync"/> for an
        /// on-demand equivalent that isn't tied to the subscription lifecycle.
        /// </summary>
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
