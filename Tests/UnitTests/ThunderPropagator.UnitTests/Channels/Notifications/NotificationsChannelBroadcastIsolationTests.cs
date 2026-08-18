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
    /// Issue #59: NotificationsChannel's broadcast fan-out used to retarget a single shared
    /// FeederMessage instance's UserId once per recipient and emit that same instance repeatedly,
    /// so every emission downstream observed whichever UserId was set last. The fix (built on
    /// #75's copy constructor) constructs a fresh NotificationsChannelFeederMessage per recipient
    /// instead. These tests observe that guarantee through AbstractChannel's public
    /// MessageEmitting event — the exact reference each recipient's emission carries downstream —
    /// which is the only externally observable seam into the per-recipient loop, since
    /// EmitMessageAsync's broadcast branch is a private implementation detail.
    /// </summary>
    public sealed class NotificationsChannelBroadcastIsolationTests
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

            // Mirrors what ChannelManager does at startup: Subscriptions is null until Initialize
            // runs. Called with snapshotting still disabled so InitializeMetadata doesn't touch
            // the recovery/backup-scheduling services this minimal service provider doesn't supply.
            channel.Initialize(CancellationToken.None);

            return channel;
        }

        /// <summary>
        /// Snapshotting has to be enabled for broadcast recipients to be discoverable at all —
        /// EmitMessageAsync's empty-UserId branch resolves recipients via SearchSnapshotsAsync,
        /// which only ever finds entries HandleSnapshotAsync stored, and that only runs when
        /// Metadata.Snapshot.IsEnabled. SetChannelSnapshot is protected with many positional
        /// defaults, so every parameter but isEnabled is left as Type.Missing to stay robust
        /// against unrelated signature changes upstream.
        /// </summary>
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

        /// <summary>
        /// Registers a broadcast snapshot entry for userId by emitting a targeted message through
        /// the channel's own public pipeline — the same mechanism a real broadcast notification
        /// would have used to reach SearchSnapshotsAsync's later "who has seen a broadcast"
        /// bookkeeping (see SendMissedBroadcastsAsync). Each call gets its own hash key because
        /// hash keys are computed from UserId (Date is left unset and hashes to null uniformly),
        /// so distinct UserIds land in distinct snapshot slots.
        /// </summary>
        private static async Task SeedBroadcastRecipientAsync(IChannel channel, string userId)
        {
            var seed = new NotificationsChannelFeederMessage { UserId = userId, CastType = CastType.Broadcast };
            await channel.EmitMessageAsync(seed, CancellationToken.None);
        }

        [Fact]
        public async Task Broadcast_EachRecipient_GetsADistinctMessageInstance()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await SeedBroadcastRecipientAsync(iChannel, "user-1");
            await SeedBroadcastRecipientAsync(iChannel, "user-2");
            await SeedBroadcastRecipientAsync(iChannel, "user-3");

            var emitted = new List<IReadOnlyDictionary<string, object?>>();
            channel.MessageEmitting += (_, feederMessage) => emitted.Add(feederMessage);

            var broadcast = new NotificationsChannelFeederMessage();
            await iChannel.EmitMessageAsync(broadcast, CancellationToken.None);

            Assert.Equal(3, emitted.Count);
            for (var i = 0; i < emitted.Count; i++)
            for (var j = i + 1; j < emitted.Count; j++)
                Assert.False(ReferenceEquals(emitted[i], emitted[j]), "Each recipient must receive its own message instance, not a shared/mutated one.");
        }

        [Fact]
        public async Task Broadcast_EachEmittedCopy_CarriesItsOwnIntendedUserId()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await SeedBroadcastRecipientAsync(iChannel, "user-1");
            await SeedBroadcastRecipientAsync(iChannel, "user-2");
            await SeedBroadcastRecipientAsync(iChannel, "user-3");

            var emitted = new List<IReadOnlyDictionary<string, object?>>();
            channel.MessageEmitting += (_, feederMessage) => emitted.Add(feederMessage);

            var broadcast = new NotificationsChannelFeederMessage();
            await iChannel.EmitMessageAsync(broadcast, CancellationToken.None);

            var deliveredUserIds = emitted
                .Select(feederMessage => ((NotificationsChannelFeederMessage)feederMessage).UserId ?? string.Empty)
                .OrderBy(userId => userId)
                .ToArray();

            Assert.Equal(["user-1", "user-2", "user-3"], deliveredUserIds);
        }

        [Fact]
        public async Task Broadcast_OriginalMessage_RemainsUnchangedAfterFanOut()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await SeedBroadcastRecipientAsync(iChannel, "user-1");
            await SeedBroadcastRecipientAsync(iChannel, "user-2");

            var broadcast = new NotificationsChannelFeederMessage();
            await iChannel.EmitMessageAsync(broadcast, CancellationToken.None);

            Assert.True(string.IsNullOrEmpty(broadcast.UserId), "The original broadcast instance must never be retargeted at a specific recipient.");
        }

        [Fact]
        public async Task TargetedDelivery_WithUserIdAlreadySet_SkipsFanOutAndKeepsTheSameInstance()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            var emitted = new List<IReadOnlyDictionary<string, object?>>();
            channel.MessageEmitting += (_, feederMessage) => emitted.Add(feederMessage);

            var targeted = new NotificationsChannelFeederMessage { UserId = "user-1" };
            await iChannel.EmitMessageAsync(targeted, CancellationToken.None);

            var single = Assert.Single(emitted);
            Assert.Same(targeted, single);
        }
    }
}
