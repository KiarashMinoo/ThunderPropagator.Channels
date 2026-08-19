using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #66: SnapshotsToSendAsync used `new CancellationToken()` as its default parameter
    /// value — functionally identical to `default`, but non-idiomatic and unclear about intent.
    /// These tests cover the actual behavior the signature promises: a supplied token propagates to
    /// the cancellable search underneath, and omitting it entirely still works via the default.
    ///
    /// Also covers issue #78's "a new subscription receives the correct snapshot" — SnapshotsToSendAsync
    /// is the exact computation the framework's own Subscribe()-triggered replay calls to decide
    /// what to enqueue for a newly established subscription (see AbstractChannel.Subscribe), so
    /// testing it directly here is the deterministic, awaitable equivalent of exercising that
    /// fire-and-forget replay path end to end.
    /// </summary>
    public sealed class NotificationsChannelSnapshotsToSendAsyncTests
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

        private static Subscription CreateSubscription(NotificationsChannel<TestNotificationsChannelConfiguration> channel, string userId)
        {
            var keyDescriptor = new SubscribingKeyChannelProgramsDescriptor(0, nameof(NotificationsChannelFeederMessage.UserId));
            var subscribedKeys = new[] { new SubscribedKey(keyDescriptor, userId) };
            IReadOnlyDictionary<string, ChannelProgramsDescriptor> subscribedFields = new Dictionary<string, ChannelProgramsDescriptor>();

            return new Subscription(Substitute.For<IConnectionInfo>(), channel, "request-1", "subscription-1", subscribedKeys, subscribedFields);
        }

        [Fact]
        public async Task SnapshotsToSendAsync_ForANewSubscription_ReturnsItsOwnStoredNotifications()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject" }, CancellationToken.None);
            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-2", Subject = "subject" }, CancellationToken.None);

            var subscription = CreateSubscription(channel, "user-1");
            var snapshot = await channel.SnapshotsToSendAsync(subscription);

            var ids = snapshot.Select(entry => entry.Snapshot[nameof(NotificationsChannelFeederMessage.Id)]).OrderBy(id => id).ToArray();
            Assert.Equal(["notification-1", "notification-2"], ids);
        }

        [Fact]
        public async Task SnapshotsToSendAsync_ForANewSubscription_ExcludesOtherRecipientsNotifications()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject" }, CancellationToken.None);
            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-2", Id = "notification-2", Subject = "subject" }, CancellationToken.None);

            var subscription = CreateSubscription(channel, "user-1");
            var snapshot = await channel.SnapshotsToSendAsync(subscription);

            var entry = Assert.Single(snapshot);
            Assert.Equal("notification-1", entry.Snapshot[nameof(NotificationsChannelFeederMessage.Id)]);
        }

        [Fact]
        public async Task SnapshotsToSendAsync_ForASubscriptionWithNoStoredNotifications_ReturnsEmpty()
        {
            var channel = CreateChannel();
            EnableSnapshotting(channel);

            var subscription = CreateSubscription(channel, "user-with-nothing-stored");
            var snapshot = await channel.SnapshotsToSendAsync(subscription);

            Assert.Empty(snapshot);
        }

        [Fact]
        public async Task SnapshotsToSendAsync_CancelledToken_PropagatesCancellation()
        {
            var channel = CreateChannel();
            var subscription = CreateSubscription(channel, "user-1");

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => channel.SnapshotsToSendAsync(subscription, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task SnapshotsToSendAsync_WithoutAnExplicitToken_UsesTheIdiomaticDefault()
        {
            var channel = CreateChannel();
            var subscription = CreateSubscription(channel, "user-1");

            var result = await channel.SnapshotsToSendAsync(subscription);

            Assert.Empty(result);
        }
    }
}
