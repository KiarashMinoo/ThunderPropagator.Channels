using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Exceptions;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Notifications;
using ThunderPropagator.Channels.Notifications.Channel;
using ThunderPropagator.Channels.Notifications.Messages;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #77: subscribers had no way to acknowledge delivery or interaction, so stored state
    /// couldn't distinguish delivered/seen/read/dismissed notifications. NotificationsChannel.AcknowledgeAsync
    /// is the core operation (usable directly by a REST endpoint or message-broker consumer that has
    /// already authenticated its caller); NotificationsAcknowledgeReceiverPipeline is a thin
    /// WebSocket-facing wrapper over the same method that instead resolves the caller's UserId from
    /// the connection's own established identity (see SubscribedUserIdsByConnectionId below) rather
    /// than trusting one supplied over the wire. These tests cover the core method's full contract:
    /// merging semantics, idempotency, concurrency safety, and every rejection case named in the
    /// issue (unauthorized, duplicate, unknown/expired Id).
    /// </summary>
    public sealed class NotificationsChannelAcknowledgementTests
    {
        public sealed class TestNotificationsChannelConfiguration : AbstractChannelConfiguration;

        private sealed class TestTimeProvider : TimeProvider
        {
            public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => UtcNow;
        }

        private sealed record CreatedChannel(
            NotificationsChannel<TestNotificationsChannelConfiguration> Channel,
            TestTimeProvider TimeProvider);

        private static CreatedChannel CreateChannel(bool isEnabled = true)
        {
            var timeProvider = new TestTimeProvider();
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(TimeProvider)).Returns(timeProvider);
            serviceProvider.GetService(typeof(TestNotificationsChannelConfiguration))
                .Returns(new TestNotificationsChannelConfiguration { IsEnabled = isEnabled });

            var channel = new NotificationsChannel<TestNotificationsChannelConfiguration>(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return new CreatedChannel(channel, timeProvider);
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

        private static async Task SeedNotificationAsync(IChannel channel, string userId, string id, DateTime? expiresAt = null)
        {
            await channel.EmitMessageAsync(
                new NotificationsChannelFeederMessage { UserId = userId, Id = id, Subject = "subject", ExpiresAt = expiresAt },
                CancellationToken.None);
        }

        [Theory]
        [InlineData(NotificationDeliveryState.Delivered)]
        [InlineData(NotificationDeliveryState.Seen)]
        [InlineData(NotificationDeliveryState.Read)]
        [InlineData(NotificationDeliveryState.Dismissed)]
        public async Task AcknowledgeAsync_WithASingleFlag_StoresAndReturnsIt(NotificationDeliveryState state)
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;
            await SeedNotificationAsync(iChannel, "user-1", "notification-1");

            var result = await created.Channel.AcknowledgeAsync("user-1", "notification-1", state);

            Assert.Equal(state, result);
            var history = await created.Channel.SearchHistoricalNotificationsAsync("user-1");
            var entry = Assert.Single(history);
            Assert.Equal(state, entry.Snapshot[nameof(NotificationsChannelFeederMessage.Seen)]);
        }

        [Fact]
        public async Task AcknowledgeAsync_AccumulatesFlagsAcrossSeparateCalls_RegardlessOfOrder()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;
            await SeedNotificationAsync(iChannel, "user-1", "notification-1");

            await created.Channel.AcknowledgeAsync("user-1", "notification-1", NotificationDeliveryState.Delivered);
            await created.Channel.AcknowledgeAsync("user-1", "notification-1", NotificationDeliveryState.Read);
            var result = await created.Channel.AcknowledgeAsync("user-1", "notification-1", NotificationDeliveryState.Seen);

            Assert.Equal(
                NotificationDeliveryState.Delivered | NotificationDeliveryState.Seen | NotificationDeliveryState.Read,
                result);
        }

        [Fact]
        public async Task AcknowledgeAsync_RepeatedWithTheSameState_IsIdempotent()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;
            await SeedNotificationAsync(iChannel, "user-1", "notification-1");

            var first = await created.Channel.AcknowledgeAsync("user-1", "notification-1", NotificationDeliveryState.Read);
            var second = await created.Channel.AcknowledgeAsync("user-1", "notification-1", NotificationDeliveryState.Read);

            Assert.Equal(NotificationDeliveryState.Read, first);
            Assert.Equal(NotificationDeliveryState.Read, second);
        }

        [Fact]
        public async Task AcknowledgeAsync_WithAWrongUserId_ThrowsUnknownNotification()
        {
            // Doubles as this method's authorization check (see #77): a caller who doesn't already
            // know the correct UserId for this Id can't distinguish "wrong user" from "doesn't exist."
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;
            await SeedNotificationAsync(iChannel, "owner", "notification-1");

            var exception = await Assert.ThrowsAsync<NotificationsChannelUnknownNotificationException>(
                () => created.Channel.AcknowledgeAsync("someone-else", "notification-1", NotificationDeliveryState.Read));

            Assert.Equal("someone-else", exception.UserId);
            Assert.Equal("notification-1", exception.Id);
        }

        [Fact]
        public async Task AcknowledgeAsync_WithAnUnknownId_ThrowsUnknownNotification()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);

            var exception = await Assert.ThrowsAsync<NotificationsChannelUnknownNotificationException>(
                () => created.Channel.AcknowledgeAsync("user-1", "never-existed", NotificationDeliveryState.Read));

            Assert.Equal("user-1", exception.UserId);
            Assert.Equal("never-existed", exception.Id);
        }

        [Fact]
        public async Task AcknowledgeAsync_ForAnExpiredNotification_ThrowsUnknownNotification()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;
            var expiresAt = created.TimeProvider.UtcNow.UtcDateTime.AddHours(1);
            await SeedNotificationAsync(iChannel, "user-1", "notification-1", expiresAt);

            created.TimeProvider.UtcNow = new DateTimeOffset(expiresAt.AddSeconds(1), TimeSpan.Zero);

            var exception = await Assert.ThrowsAsync<NotificationsChannelUnknownNotificationException>(
                () => created.Channel.AcknowledgeAsync("user-1", "notification-1", NotificationDeliveryState.Read));

            Assert.Equal("notification-1", exception.Id);
        }

        [Fact]
        public async Task AcknowledgeAsync_WhileChannelDisabled_ThrowsChannelIsNotEnabled()
        {
            var created = CreateChannel(isEnabled: false);

            await Assert.ThrowsAsync<ChannelIsNotEnabledException>(
                () => created.Channel.AcknowledgeAsync("user-1", "notification-1", NotificationDeliveryState.Read));
        }

        [Fact]
        public async Task AcknowledgeAsync_WithAnUndefinedFlagCombination_ThrowsValidation()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;
            await SeedNotificationAsync(iChannel, "user-1", "notification-1");

            var invalidState = (NotificationDeliveryState)64;

            await Assert.ThrowsAsync<NotificationsChannelFeederMessageValidationException>(
                () => created.Channel.AcknowledgeAsync("user-1", "notification-1", invalidState));
        }

        [Fact]
        public async Task AcknowledgeAsync_UndefinedFlagCombination_FailsBeforeSearchingForTheNotification()
        {
            // The invalid state is rejected even for an Id that was never emitted at all — validation
            // runs before any lookup, so this never surfaces as "unknown notification" instead.
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);

            var invalidState = (NotificationDeliveryState)64;

            await Assert.ThrowsAsync<NotificationsChannelFeederMessageValidationException>(
                () => created.Channel.AcknowledgeAsync("user-1", "never-existed", invalidState));
        }

        [Fact]
        public async Task AcknowledgeAsync_ConcurrentDistinctFlags_MergeWithoutLosingEither()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;
            await SeedNotificationAsync(iChannel, "user-1", "notification-1");

            var readTask = created.Channel.AcknowledgeAsync("user-1", "notification-1", NotificationDeliveryState.Read);
            var dismissedTask = created.Channel.AcknowledgeAsync("user-1", "notification-1", NotificationDeliveryState.Dismissed);
            await Task.WhenAll(readTask, dismissedTask);

            var history = await created.Channel.SearchHistoricalNotificationsAsync("user-1");
            var entry = Assert.Single(history);
            Assert.Equal(
                NotificationDeliveryState.Read | NotificationDeliveryState.Dismissed,
                entry.Snapshot[nameof(NotificationsChannelFeederMessage.Seen)]);
        }

        [Fact]
        public async Task AcknowledgeAsync_ManyConcurrentAcknowledgements_AllFlagsSurviveTheMerge()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;
            await SeedNotificationAsync(iChannel, "user-1", "notification-1");

            NotificationDeliveryState[] states =
            [
                NotificationDeliveryState.Delivered,
                NotificationDeliveryState.Seen,
                NotificationDeliveryState.Read,
                NotificationDeliveryState.Dismissed
            ];

            await Task.WhenAll(states.Select(state => created.Channel.AcknowledgeAsync("user-1", "notification-1", state)));

            var history = await created.Channel.SearchHistoricalNotificationsAsync("user-1");
            var entry = Assert.Single(history);
            var expected = states.Aggregate((a, b) => a | b);
            Assert.Equal(expected, entry.Snapshot[nameof(NotificationsChannelFeederMessage.Seen)]);
        }

        private static Subscription CreateSubscription(NotificationsChannel<TestNotificationsChannelConfiguration> channel, string userId, string connectionId)
        {
            var keyDescriptor = new SubscribingKeyChannelProgramsDescriptor(0, nameof(NotificationsChannelFeederMessage.UserId));
            var subscribedKeys = new[] { new SubscribedKey(keyDescriptor, userId) };
            IReadOnlyDictionary<string, ChannelProgramsDescriptor> subscribedFields = new Dictionary<string, ChannelProgramsDescriptor>();

            var connectionInfo = Substitute.For<IConnectionInfo>();
            connectionInfo.ConnectionId.Returns(connectionId);

            return new Subscription(connectionInfo, channel, "request-1", "subscription-1", subscribedKeys, subscribedFields);
        }

        private static void InvokeOnSubscriptionAdded(NotificationsChannel<TestNotificationsChannelConfiguration> channel, Subscription subscription)
        {
            var method = typeof(NotificationsChannel<TestNotificationsChannelConfiguration>).GetMethod("OnSubscriptionAdded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingMethodException(typeof(NotificationsChannel<TestNotificationsChannelConfiguration>).FullName, "OnSubscriptionAdded");
            method.Invoke(channel, [subscription]);
        }

        private static void InvokeOnSubscriptionRemoved(NotificationsChannel<TestNotificationsChannelConfiguration> channel, Subscription subscription)
        {
            var method = typeof(NotificationsChannel<TestNotificationsChannelConfiguration>).GetMethod("OnSubscriptionRemoved", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingMethodException(typeof(NotificationsChannel<TestNotificationsChannelConfiguration>).FullName, "OnSubscriptionRemoved");
            method.Invoke(channel, [subscription]);
        }

        [Fact]
        public void OnSubscriptionAdded_RecordsTheConnectionsUserId()
        {
            var created = CreateChannel();
            var subscription = CreateSubscription(created.Channel, "user-1", "connection-1");

            InvokeOnSubscriptionAdded(created.Channel, subscription);

            Assert.True(created.Channel.SubscribedUserIdsByConnectionId.TryGetValue("connection-1", out var userId));
            Assert.Equal("user-1", userId);
        }

        [Fact]
        public void OnSubscriptionRemoved_ForgetsTheConnectionsUserId()
        {
            var created = CreateChannel();
            var subscription = CreateSubscription(created.Channel, "user-1", "connection-1");
            InvokeOnSubscriptionAdded(created.Channel, subscription);

            InvokeOnSubscriptionRemoved(created.Channel, subscription);

            Assert.False(created.Channel.SubscribedUserIdsByConnectionId.ContainsKey("connection-1"));
        }

        [Fact]
        public void SubscribedUserIdsByConnectionId_ForAConnectionThatNeverSubscribed_HasNoEntry()
        {
            var created = CreateChannel();

            Assert.False(created.Channel.SubscribedUserIdsByConnectionId.ContainsKey("never-subscribed"));
        }
    }
}
