using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.BuildingBlocks.Application.Enums;
using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #74: a message with UserId unset but GroupId set routes only to recipients already
    /// known to be members of that group — reusing the same CastType.Broadcast-tagged snapshot
    /// storage plain broadcasts already use, scoped by an exact GroupId match — rather than to every
    /// known broadcast recipient the way a GroupId-less broadcast does.
    ///
    /// Along the way this surfaced a pre-existing gap in the plain-broadcast fan-out: recipient
    /// copies inherited whatever CastType the original (unrouted) broadcast message happened to
    /// carry — Multicast by default, since callers rarely set CastType themselves — leaving those
    /// copies invisible to future fan-out discovery and to missed-broadcast catch-up, both of which
    /// filter on CastType.Broadcast. EmitMessageAsync now forces CastType.Broadcast on every
    /// recipient copy it creates, fixing that for plain and group-scoped broadcasts alike. These
    /// tests exercise that fix as a prerequisite for group routing actually working, and confirm
    /// missed-broadcast catch-up only replays a group-scoped entry to users already known to be
    /// members of that group — never to a brand-new subscriber purely by virtue of reconnecting,
    /// which would otherwise leak every group's history to any new subscriber.
    /// </summary>
    public sealed class NotificationsChannelGroupRoutingTests
    {
        public sealed class TestNotificationsChannelConfiguration : AbstractChannelConfiguration;

        private static NotificationsChannel<TestNotificationsChannelConfiguration> CreateChannel()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(TestNotificationsChannelConfiguration))
                .Returns(new TestNotificationsChannelConfiguration { IsEnabled = true });

            var channel = new NotificationsChannel<TestNotificationsChannelConfiguration>(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return channel;
        }

        private static void EnableSnapshotting(NotificationsChannel<TestNotificationsChannelConfiguration> channel)
        {
            for (var type = channel.Metadata.GetType(); type is not null; type = type.BaseType)
            {
                var method = type.GetMethod("SetChannelSnapshot", BindingFlags.NonPublic | BindingFlags.Instance);
                if (method is null)
                    continue;

                var arguments = method.GetParameters()
                    .Select(parameter => parameter.Name == "isEnabled" ? true : Type.Missing)
                    .ToArray();

                method.Invoke(channel.Metadata,
                    BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.OptionalParamBinding,
                    null, arguments, null);
                return;
            }

            throw new MissingMethodException(channel.Metadata.GetType().FullName, "SetChannelSnapshot");
        }

        private static Task InvokeSendMissedBroadcastsAsync(NotificationsChannel<TestNotificationsChannelConfiguration> channel, string userId)
        {
            var method = typeof(NotificationsChannel<TestNotificationsChannelConfiguration>).GetMethod("SendMissedBroadcastsAsync", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingMethodException(typeof(NotificationsChannel<TestNotificationsChannelConfiguration>).FullName, "SendMissedBroadcastsAsync");
            return (Task)method.Invoke(channel, [userId, CancellationToken.None])!;
        }

        /// <summary>
        /// Registers userId as a known member of groupId by emitting a targeted, CastType.Broadcast-tagged
        /// message through the channel's own public pipeline — the same bootstrapping mechanism
        /// NotificationsChannelBroadcastIsolationTests already uses to seed plain broadcast
        /// recipients, extended with a GroupId.
        /// </summary>
        private static async Task SeedGroupMemberAsync(IChannel channel, string userId, string groupId)
        {
            var seed = new NotificationsChannelFeederMessage { UserId = userId, CastType = CastType.Broadcast, Id = $"seed-{userId}-{groupId}", Subject = "subject", GroupId = groupId };
            await channel.EmitMessageAsync(seed, CancellationToken.None);
        }

        [Fact]
        public async Task GroupScopedBroadcast_DeliversOnlyToKnownGroupMembers()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await SeedGroupMemberAsync(iChannel, "member-1", "group-a");
            await SeedGroupMemberAsync(iChannel, "member-2", "group-a");
            await SeedGroupMemberAsync(iChannel, "outsider", "group-b");

            var emitted = new List<IReadOnlyDictionary<string, object?>>();
            channel.MessageEmitting += (_, feederMessage) => emitted.Add(feederMessage);

            var groupBroadcast = new NotificationsChannelFeederMessage { Id = "group-a-announcement", Subject = "subject", GroupId = "group-a" };
            await iChannel.EmitMessageAsync(groupBroadcast, CancellationToken.None);

            var recipients = emitted
                .Select(feederMessage => ((NotificationsChannelFeederMessage)feederMessage).UserId!)
                .OrderBy(userId => userId)
                .ToArray();

            Assert.Equal(["member-1", "member-2"], recipients);
        }

        [Fact]
        public async Task GroupScopedBroadcast_RecipientCopies_CarryTheGroupId()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await SeedGroupMemberAsync(iChannel, "member-1", "group-a");

            var emitted = new List<IReadOnlyDictionary<string, object?>>();
            channel.MessageEmitting += (_, feederMessage) => emitted.Add(feederMessage);

            var groupBroadcast = new NotificationsChannelFeederMessage { Id = "group-a-announcement", Subject = "subject", GroupId = "group-a" };
            await iChannel.EmitMessageAsync(groupBroadcast, CancellationToken.None);

            var delivered = Assert.Single(emitted);
            Assert.Equal("group-a", ((NotificationsChannelFeederMessage)delivered).GroupId);
        }

        [Fact]
        public async Task PlainBroadcast_StillReachesEveryKnownRecipient_RegardlessOfGroupMembership()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await SeedGroupMemberAsync(iChannel, "member-1", "group-a");
            await SeedGroupMemberAsync(iChannel, "outsider", "group-b");

            var emitted = new List<IReadOnlyDictionary<string, object?>>();
            channel.MessageEmitting += (_, feederMessage) => emitted.Add(feederMessage);

            var plainBroadcast = new NotificationsChannelFeederMessage { Id = "plain-announcement", Subject = "subject" };
            await iChannel.EmitMessageAsync(plainBroadcast, CancellationToken.None);

            var recipients = emitted
                .Select(feederMessage => ((NotificationsChannelFeederMessage)feederMessage).UserId!)
                .OrderBy(userId => userId)
                .ToArray();

            Assert.Equal(["member-1", "outsider"], recipients);
        }

        [Fact]
        public async Task PlainBroadcast_RecipientCopies_AreStoredAsCastTypeBroadcast()
        {
            // Regression coverage for the pre-existing CastType propagation gap this ticket fixed:
            // a plain broadcast's own CastType is never explicitly set by a typical caller (defaults
            // to Multicast), so without forcing CastType.Broadcast on the recipient copy, it would
            // never be discoverable again by a later broadcast's fan-out or by catch-up.
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await SeedGroupMemberAsync(iChannel, "member-1", "group-a");

            var plainBroadcast = new NotificationsChannelFeederMessage { Id = "plain-announcement", Subject = "subject" };
            await iChannel.EmitMessageAsync(plainBroadcast, CancellationToken.None);

            var history = await channel.SearchHistoricalNotificationsAsync("member-1");
            var storedEntry = Assert.Single(history, entry => Equals(entry.Snapshot[nameof(NotificationsChannelFeederMessage.Id)], "plain-announcement"));

            Assert.Equal(CastType.Broadcast, storedEntry.CastType);
        }

        [Fact]
        public async Task MissedBroadcastCatchUp_ReplaysAGroupScopedEntry_ToAMemberWhoJoinedAfterItWasSent()
        {
            // Fan-out stores a copy for every known member the instant a group message is emitted,
            // independent of live connection state — so an *already*-a-member recipient is never
            // "missing" anything by the time it reconnects; there's nothing to catch up on. The
            // realistic case catch-up actually serves: member-1 joins group-a *after* an
            // announcement already went out to its existing members, and only later subscribes —
            // it should still see that pre-membership announcement once it does.
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await SeedGroupMemberAsync(iChannel, "member-2", "group-a");
            var groupBroadcast = new NotificationsChannelFeederMessage { Id = "group-a-announcement", Subject = "subject", GroupId = "group-a" };
            await iChannel.EmitMessageAsync(groupBroadcast, CancellationToken.None);

            await SeedGroupMemberAsync(iChannel, "member-1", "group-a");

            var emitted = new List<IReadOnlyDictionary<string, object?>>();
            channel.MessageEmitting += (_, feederMessage) => emitted.Add(feederMessage);

            await InvokeSendMissedBroadcastsAsync(channel, "member-1");

            Assert.Contains(emitted, feederMessage => Equals(feederMessage[nameof(NotificationsChannelFeederMessage.Id)], "group-a-announcement"));
        }

        [Fact]
        public async Task MissedBroadcastCatchUp_NeverReplaysAGroupScopedEntry_ToANonMember()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await SeedGroupMemberAsync(iChannel, "member-1", "group-a");

            var groupBroadcast = new NotificationsChannelFeederMessage { Id = "group-a-announcement", Subject = "subject", GroupId = "group-a" };
            await iChannel.EmitMessageAsync(groupBroadcast, CancellationToken.None);

            var emitted = new List<IReadOnlyDictionary<string, object?>>();
            channel.MessageEmitting += (_, feederMessage) => emitted.Add(feederMessage);

            // brand-new-subscriber, never a member of group-a (or anything else) — must not be
            // caught up on group-a's history purely by virtue of subscribing for the first time.
            await InvokeSendMissedBroadcastsAsync(channel, "brand-new-subscriber");

            Assert.DoesNotContain(emitted, feederMessage => Equals(feederMessage[nameof(NotificationsChannelFeederMessage.Id)], "group-a-announcement"));
        }

        [Fact]
        public async Task MissedBroadcastCatchUp_StillReplaysPlainBroadcasts_ToABrandNewSubscriber()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await SeedGroupMemberAsync(iChannel, "member-1", "group-a");

            var plainBroadcast = new NotificationsChannelFeederMessage { Id = "plain-announcement", Subject = "subject" };
            await iChannel.EmitMessageAsync(plainBroadcast, CancellationToken.None);

            var emitted = new List<IReadOnlyDictionary<string, object?>>();
            channel.MessageEmitting += (_, feederMessage) => emitted.Add(feederMessage);

            await InvokeSendMissedBroadcastsAsync(channel, "brand-new-subscriber");

            Assert.Contains(emitted, feederMessage => Equals(feederMessage[nameof(NotificationsChannelFeederMessage.Id)], "plain-announcement"));
        }

        [Fact]
        public async Task SearchHistoricalNotificationsAsync_WithGroupIdFilter_ReturnsOnlyThatGroupsNotifications()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "in-group", Subject = "subject", GroupId = "group-a" }, CancellationToken.None);
            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "no-group", Subject = "subject" }, CancellationToken.None);

            var history = await channel.SearchHistoricalNotificationsAsync("user-1", groupId: "group-a");

            var entry = Assert.Single(history);
            Assert.Equal("in-group", entry.Snapshot[nameof(NotificationsChannelFeederMessage.Id)]);
        }

        [Fact]
        public async Task SearchHistoricalNotificationsAsync_WithSingleTagFilter_ReturnsMatchingNotifications()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "tagged", Subject = "subject", Tags = ["urgent"] }, CancellationToken.None);
            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "untagged", Subject = "subject" }, CancellationToken.None);

            var history = await channel.SearchHistoricalNotificationsAsync("user-1", tags: ["urgent"]);

            var entry = Assert.Single(history);
            Assert.Equal("tagged", entry.Snapshot[nameof(NotificationsChannelFeederMessage.Id)]);
        }

        [Fact]
        public async Task SearchHistoricalNotificationsAsync_WithMultipleTagFilter_MatchesAnyRequestedTag()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "billing-only", Subject = "subject", Tags = ["billing"] }, CancellationToken.None);
            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "urgent-only", Subject = "subject", Tags = ["urgent"] }, CancellationToken.None);
            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "neither", Subject = "subject", Tags = ["marketing"] }, CancellationToken.None);

            var history = await channel.SearchHistoricalNotificationsAsync("user-1", tags: ["billing", "urgent"]);

            var ids = history.Select(entry => entry.Snapshot[nameof(NotificationsChannelFeederMessage.Id)]).OrderBy(id => id).ToArray();
            Assert.Equal(["billing-only", "urgent-only"], ids);
        }

        [Fact]
        public async Task SearchHistoricalNotificationsAsync_TagFilterIsCaseInsensitive()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "tagged", Subject = "subject", Tags = ["Urgent"] }, CancellationToken.None);

            var history = await channel.SearchHistoricalNotificationsAsync("user-1", tags: ["urgent"]);

            Assert.Single(history);
        }

        [Fact]
        public async Task SearchHistoricalNotificationsAsync_WithNoTagFilter_ReturnsUntaggedNotificationsToo()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "untagged", Subject = "subject" }, CancellationToken.None);

            var history = await channel.SearchHistoricalNotificationsAsync("user-1");

            Assert.Single(history);
        }
    }
}
