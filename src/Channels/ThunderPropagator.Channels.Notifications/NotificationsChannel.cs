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
    /// expected to honor. Routing follows <see cref="NotificationsChannelFeederMessage.Audience"/>
    /// (see #76): <see cref="NotificationAudience.Individual"/> delivers to that one recipient;
    /// <see cref="NotificationAudience.Broadcast"/> reaches every current subscriber, and a
    /// subscriber who joins afterward is caught up on broadcasts it missed;
    /// <see cref="NotificationAudience.Group"/> narrows that catch-up and delivery to recipients
    /// already known to belong to the given group (see #74). See
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
    /// <para><b>Per-message expiration (see #73):</b> <see cref="NotificationsChannelFeederMessage.ExpiresAt"/>
    /// is evaluated fresh, per call, against a <see cref="TimeProvider"/> resolved once at
    /// construction — never cached across calls. An expired entry is excluded from snapshot replay,
    /// from <see cref="SearchHistoricalNotificationsAsync"/>, and from missed-broadcast catch-up;
    /// a message that's already expired at the moment <c>EmitMessage</c>/<c>EmitMessageAsync</c> is
    /// called is skipped (logged, not thrown — unlike the <c>IsEnabled</c> checks above, an expired
    /// message isn't a caller error). The boundary is inclusive: a message is expired the instant the
    /// clock reaches <see cref="NotificationsChannelFeederMessage.ExpiresAt"/>, not strictly after
    /// it. This is independent of <see cref="NotificationsFeederConfiguration.TimeToLive"/>, which
    /// the channel never reads directly — see that property's remarks for how a feeder translates
    /// its own default TTL into a per-message <see cref="NotificationsChannelFeederMessage.ExpiresAt"/>.</para>
    /// <para><b>Group-scoped routing (see #74):</b> a message with <see cref="NotificationsChannelFeederMessage.UserId"/>
    /// unset but <see cref="NotificationsChannelFeederMessage.GroupId"/> set is routed only to
    /// recipients already known to be members of that group, rather than to every current
    /// subscriber the way an ordinary broadcast is. Membership is implicit: a recipient becomes a
    /// known member of a group the same way it becomes a known broadcast recipient at all — by
    /// having previously received a targeted, <c>CastType.Broadcast</c>-tagged message carrying that
    /// GroupId. Missed-broadcast catch-up respects this too: a newly (re)subscribed recipient is
    /// caught up on every plain (GroupId-less) broadcast it missed, as before, but only on a
    /// group-scoped broadcast for a group it's already a known member of — never on a group's
    /// history purely by virtue of reconnecting. <see cref="NotificationsChannelFeederMessage.Tags"/>
    /// carries no routing behavior of its own; see <see cref="SearchHistoricalNotificationsAsync"/>
    /// for filtering stored history by tag or GroupId.</para>
    /// </remarks>
    public
#if !DEBUG
        sealed
#endif
        class NotificationsChannel<TNotificationsChannelConfiguration> : AbstractChannel<NotificationsChannelMetadata<TNotificationsChannelConfiguration>, TNotificationsChannelConfiguration>
        where TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()
    {
        private readonly CancellationToken _cancellationToken;
        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// Resolves the shared application-stopping token used to cancel background/fire-and-forget
        /// work started by this channel, and the <see cref="TimeProvider"/> used to evaluate
        /// <see cref="NotificationsChannelFeederMessage.ExpiresAt"/> (see #73) — falling back to
        /// <see cref="TimeProvider.System"/> when nothing is registered, so existing hosts that never
        /// registered one keep working unchanged. Tests inject a fake one via DI for deterministic
        /// expiration behavior.
        /// </summary>
        public NotificationsChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _cancellationToken = serviceProvider.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
            _timeProvider = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
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
                // Group-scoped broadcasts (see #74) share the same CastType.Broadcast storage pool
                // as plain broadcasts, distinguished only by GroupId — so catch-up must know which
                // groups userId already belongs to before deciding whether a group-scoped entry is
                // eligible for replay. Without this, a brand-new subscriber would be caught up on
                // every group's past messages (having "missed" all of them), leaking group content
                // to users who were never members. A plain (GroupId-less) entry has no such
                // restriction and remains eligible for anyone, as before.
                var memberGroupIds = (await SearchSnapshotsAsync(snapshotEntries => snapshotEntries
                            .Where(snapshotEntry => userId.Equals(snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.UserId)]?.ToString()))
                            .Where(snapshotEntry => GroupIdOf(snapshotEntry.Snapshot) is not null),
                        0,
                        0,
                        cancellationToken)
                    .ConfigureAwait(false))
                    .Select(snapshotEntry => GroupIdOf(snapshotEntry.Snapshot)!)
                    .ToHashSet(StringComparer.Ordinal);

                var snapshotEntries = await SearchSnapshotsAsync(snapshotEntries => snapshotEntries
                            .Where(snapshotEntry => snapshotEntry.CastType == CastType.Broadcast && !IsExpired(snapshotEntry.Snapshot))
                            .Where(snapshotEntry => GroupIdOf(snapshotEntry.Snapshot) is not { } groupId || memberGroupIds.Contains(groupId))
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
        /// <see cref="NotificationsChannelFeederMessage.Audience"/> (see #76):
        /// <see cref="NotificationAudience.Individual"/> emits it directly to
        /// <see cref="NotificationsChannelFeederMessage.UserId"/>; <see cref="NotificationAudience.Group"/>
        /// and <see cref="NotificationAudience.Broadcast"/> fan it out — a fresh per-recipient copy
        /// for each subscriber known via stored snapshots (narrowed to group members for Group), so
        /// <paramref name="feederMessage"/> itself is never mutated and no two recipients share the
        /// same emitted instance. Throws <see cref="NotificationsChannelFeederMessageValidationException"/>
        /// before doing anything else if <see cref="NotificationsChannelFeederMessage.Id"/> or
        /// <see cref="NotificationsChannelFeederMessage.Subject"/> was never set (see #68), or if
        /// <see cref="NotificationsChannelFeederMessage.Audience"/>'s required/forbidden combination
        /// with <see cref="NotificationsChannelFeederMessage.UserId"/>/<see cref="NotificationsChannelFeederMessage.GroupId"/>
        /// is violated — see <see cref="NotificationsChannelFeederMessage.ValidateAudienceCombination"/>.
        /// Both checks run at this earliest point a message built via the public parameterless
        /// constructor and object initializer can reliably be checked, since an unset property never
        /// invokes its own validating setter.
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
            notificationsChannelFeederMessage.ValidateAudienceCombination();

            if (IsExpired(notificationsChannelFeederMessage.ExpiresAt))
            {
                // Expiry is an expected, benign outcome that occurs naturally over time (e.g. a
                // delayed feeder retry), not a caller error — skipped with a log rather than thrown,
                // unlike the IsEnabled check above. Checked once here, before branching on UserId, so
                // an already-expired broadcast never produces any per-recipient copies either (the
                // copy constructor would otherwise propagate the same ExpiresAt to every copy).
                Logger.LogInformation("Skipped emitting message {Id} on channel {ChannelName} because it is already expired.", notificationsChannelFeederMessage.Id, Metadata.ChannelName);
                return;
            }

            if (notificationsChannelFeederMessage.Audience != NotificationAudience.Individual)
            {
                // Group narrows discovery to recipients already known to be members of that group (a
                // prior CastType.Broadcast-tagged entry carrying the same GroupId); Broadcast leaves
                // GroupId null (enforced by ValidateAudienceCombination above), matching every known
                // broadcast recipient instead (see #74, #76).
                var groupId = notificationsChannelFeederMessage.GroupId;
                var snapshotEntries = await SearchSnapshotsAsync(snapshotEntries => snapshotEntries
                            .Where(snapshotEntry => snapshotEntry.CastType == CastType.Broadcast)
                            .Where(snapshotEntry => string.IsNullOrWhiteSpace(groupId) || string.Equals(groupId, GroupIdOf(snapshotEntry.Snapshot), StringComparison.Ordinal))
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

                    // Forced explicitly rather than left to whatever the original (unrouted)
                    // broadcast/group message's own CastType happened to be — a caller sending a
                    // plain broadcast rarely sets CastType themselves, and the copy constructor would
                    // otherwise propagate that untouched default (Multicast). Without this, a
                    // recipient's stored copy is invisible to future fan-out discovery and to
                    // missed-broadcast catch-up, both of which filter on CastType.Broadcast (see #74).
                    recipientMessage.CastType = CastType.Broadcast;

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
            => SearchSnapshotsAsync(snapshotEntry => subscription.SubscribedPrograms.SubscribedKeys.IsEquals(snapshotEntry.Snapshot) && !IsExpired(snapshotEntry.Snapshot),
                0,
                0,
                cancellationToken);

        /// <summary>
        /// Looks up a recipient's stored notifications, optionally narrowed to a date range, a set
        /// of tags, and/or a GroupId. UserId is the only live subscription key (see #61); this is a
        /// separate, explicit query path for historical retrieval and has no effect on subscription
        /// identity or routing. Leaving <paramref name="dateRange"/> null returns the recipient's
        /// full history. Entries with no recorded Date are excluded whenever a range is supplied,
        /// since there's nothing to compare against.
        /// </summary>
        /// <param name="userId">The recipient whose stored history to search.</param>
        /// <param name="dateRange">Optional inclusive date-range filter; null returns every date.</param>
        /// <param name="tags">
        /// Optional tag filter (see #74): a notification matches if it carries <i>any</i> of the
        /// given tags (case-insensitive) — not all of them. Null or empty returns notifications
        /// regardless of their tags, including untagged ones.
        /// </param>
        /// <param name="groupId">
        /// Optional exact, case-sensitive GroupId filter (see #74); null returns notifications
        /// regardless of GroupId, including those with none.
        /// </param>
        /// <param name="cancellationToken"></param>
        public Task<SnapshotEntry[]> SearchHistoricalNotificationsAsync(
            string userId,
            NotificationsHistoricalDateRangeFilter? dateRange = null,
            IReadOnlyList<string>? tags = null,
            string? groupId = null,
            CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(userId);

            return SearchSnapshotsAsync(snapshotEntry =>
                    userId.Equals(snapshotEntry.Snapshot[nameof(NotificationsChannelFeederMessage.UserId)]?.ToString()) &&
                    !IsExpired(snapshotEntry.Snapshot) &&
                    MatchesAnyTag(snapshotEntry.Snapshot, tags) &&
                    (groupId is null || string.Equals(groupId, GroupIdOf(snapshotEntry.Snapshot), StringComparison.Ordinal)) &&
                    (dateRange is null ||
                     (snapshotEntry.Snapshot.TryGetValue(nameof(NotificationsChannelFeederMessage.Date), out var date) &&
                      date is DateTime dateTime &&
                      dateRange.IsSatisfiedBy(dateTime))),
                0,
                0,
                cancellationToken);
        }

        /// <summary>
        /// The value of <see cref="NotificationsChannelFeederMessage.GroupId"/> as stored in
        /// <paramref name="snapshot"/>, or null when absent, blank, or not a string.
        /// </summary>
        private static string? GroupIdOf(IReadOnlyDictionary<string, object?> snapshot)
            => snapshot.TryGetValue(nameof(NotificationsChannelFeederMessage.GroupId), out var value) && value is string { Length: > 0 } groupId && !string.IsNullOrWhiteSpace(groupId)
                ? groupId
                : null;

        /// <summary>
        /// Whether <paramref name="snapshot"/> carries at least one of <paramref name="tags"/>
        /// (case-insensitive) — match-any (OR), not match-all. Null or empty <paramref name="tags"/>
        /// always matches, including a snapshot with no <see cref="NotificationsChannelFeederMessage.Tags"/>
        /// entry at all.
        /// </summary>
        private static bool MatchesAnyTag(IReadOnlyDictionary<string, object?> snapshot, IReadOnlyList<string>? tags)
            => tags is null || tags.Count == 0 ||
               (snapshot.TryGetValue(nameof(NotificationsChannelFeederMessage.Tags), out var value) &&
                value is IEnumerable<string> storedTags &&
                tags.Any(tag => storedTags.Contains(tag, StringComparer.OrdinalIgnoreCase)));

        /// <summary>
        /// Whether <paramref name="snapshot"/>'s <see cref="NotificationsChannelFeederMessage.ExpiresAt"/>
        /// field, if present, is at or before the current instant per this channel's
        /// <see cref="TimeProvider"/> (see #73). A snapshot with no ExpiresAt entry, or one that
        /// failed to deserialize as a <see cref="DateTime"/>, is treated as never expired.
        /// </summary>
        private bool IsExpired(IReadOnlyDictionary<string, object?> snapshot)
            => snapshot.TryGetValue(nameof(NotificationsChannelFeederMessage.ExpiresAt), out var value) &&
               value is DateTime expiresAt &&
               IsExpired(expiresAt);

        /// <summary>
        /// Whether <paramref name="expiresAt"/> is at or before the current instant per this
        /// channel's <see cref="TimeProvider"/> — inclusive, so a value exactly equal to "now" counts
        /// as expired (see #73). Null (never set) is never expired.
        /// </summary>
        private bool IsExpired(DateTime? expiresAt)
            => expiresAt is { } value && value <= _timeProvider.GetUtcNow().UtcDateTime;
    }
}
