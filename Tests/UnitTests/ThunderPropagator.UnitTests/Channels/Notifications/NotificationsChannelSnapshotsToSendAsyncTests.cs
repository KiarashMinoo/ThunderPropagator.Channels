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

        private static Subscription CreateSubscription(NotificationsChannel<TestNotificationsChannelConfiguration> channel, string userId)
        {
            var keyDescriptor = new SubscribingKeyChannelProgramsDescriptor(0, nameof(NotificationsChannelFeederMessage.UserId));
            var subscribedKeys = new[] { new SubscribedKey(keyDescriptor, userId) };
            IReadOnlyDictionary<string, ChannelProgramsDescriptor> subscribedFields = new Dictionary<string, ChannelProgramsDescriptor>();

            return new Subscription(Substitute.For<IConnectionInfo>(), channel, "request-1", "subscription-1", subscribedKeys, subscribedFields);
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
