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

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// NotificationsChannel.OnSubscriptionAdded and EmitMessage used to block on
    /// SearchSnapshotsAsync(...).Result. These tests confirm both are fixed: no sync-over-async call
    /// remains, and neither hook lets an exception from the (now fire-and-forget) async chain
    /// propagate back to the synchronous caller — the same deadlock/thread-pool-starvation risk the
    /// issue describes would otherwise resurface if either reverted to blocking on the async result.
    /// </summary>
    public sealed class NotificationsChannelAsyncTests
    {
        public sealed class TestNotificationsChannelConfiguration : AbstractChannelConfiguration;

        private static NotificationsChannel<TestNotificationsChannelConfiguration> CreateChannel(bool isEnabled)
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(TestNotificationsChannelConfiguration))
                .Returns(new TestNotificationsChannelConfiguration { IsEnabled = isEnabled });

            return new NotificationsChannel<TestNotificationsChannelConfiguration>(serviceProvider);
        }

        private static Subscription CreateSubscription(NotificationsChannel<TestNotificationsChannelConfiguration> channel, string userId)
        {
            var keyDescriptor = new SubscribingKeyChannelProgramsDescriptor(0, nameof(NotificationsChannelFeederMessage.UserId));
            var subscribedKeys = new[] { new SubscribedKey(keyDescriptor, userId) };
            IReadOnlyDictionary<string, ChannelProgramsDescriptor> subscribedFields = new Dictionary<string, ChannelProgramsDescriptor>();

            return new Subscription(Substitute.For<IConnectionInfo>(), channel, "request-1", "subscription-1", subscribedKeys, subscribedFields);
        }

        private static void InvokeOnSubscriptionAdded(NotificationsChannel<TestNotificationsChannelConfiguration> channel, Subscription subscription)
        {
            var method = typeof(NotificationsChannel<TestNotificationsChannelConfiguration>).GetMethod("OnSubscriptionAdded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingMethodException(typeof(NotificationsChannel<TestNotificationsChannelConfiguration>).FullName, "OnSubscriptionAdded");
            method.Invoke(channel, [subscription]);
        }

        [Fact]
        public void OnSubscriptionAdded_ChannelDisabled_DoesNotThrowSynchronously()
        {
            // SearchSnapshotsAsync throws ChannelIsNotEnabledException as soon as it's awaited. The
            // old .Result-based code propagated that synchronously out of OnSubscriptionAdded (and
            // therefore out of the caller's Subscribe() call); the fixed fire-and-forget code must
            // catch and log it internally instead.
            var channel = CreateChannel(isEnabled: false);
            var subscription = CreateSubscription(channel, "user-1");

            var exception = Record.Exception(() => InvokeOnSubscriptionAdded(channel, subscription));

            Assert.Null(exception);
        }

        [Fact]
        public void EmitMessage_BroadcastWithoutUserId_ChannelDisabled_DoesNotThrowSynchronously()
        {
            // Same failure mode as above, through the EmitMessage(FeederMessage) sync entry point:
            // an empty UserId routes through SearchSnapshotsAsync to resolve broadcast recipients.
            var channel = CreateChannel(isEnabled: false);
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject" };

            var exception = Record.Exception(() => iChannel.EmitMessage(message));

            Assert.Null(exception);
        }

        [Fact]
        public void EmitMessage_WithUserIdAlreadySet_ChannelDisabled_DoesNotThrowSynchronously()
        {
            // The non-broadcast path used to skip SearchSnapshotsAsync entirely and go straight to
            // the base class's EmitMessageAsync, which no-ops (rather than throws) when disabled —
            // an inconsistency with the broadcast path's throw, fixed in #72 by checking IsEnabled
            // explicitly up front for both paths alike. Either way, the sync wrapper's
            // IsCompletedSuccessfully/ContinueWith pattern (see EmitMessage below) keeps a fault from
            // this async method — old no-op or new throw — from propagating synchronously here.
            var channel = CreateChannel(isEnabled: false);
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject" };

            var exception = Record.Exception(() => iChannel.EmitMessage(message));

            Assert.Null(exception);
        }

        [Fact]
        public async Task EmitMessageAsync_BroadcastWithoutUserId_ChannelDisabled_SurfacesExceptionOnlyViaTheTask()
        {
            // Asynchronous end to end: the failure must surface through the returned Task (awaitable
            // by a caller that opts in), not as a synchronous throw from the call expression itself.
            var channel = CreateChannel(isEnabled: false);
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject" };

            // The call expression itself must not throw — only the returned Task may fault.
            var callException = Record.Exception(() =>
            {
                var task = iChannel.EmitMessageAsync(message, CancellationToken.None);
                // Observe the fault so it can't surface later as an unobserved task exception.
                _ = task.ContinueWith(t => _ = t.Exception, TaskScheduler.Default);
            });
            Assert.Null(callException);

            await Assert.ThrowsAsync<ChannelIsNotEnabledException>(() => iChannel.EmitMessageAsync(message, CancellationToken.None));
        }

        [Fact]
        public async Task EmitMessageAsync_BroadcastWithoutUserId_CancelledToken_PropagatesCancellation()
        {
            var channel = CreateChannel(isEnabled: true);
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Audience = NotificationAudience.Broadcast };

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => iChannel.EmitMessageAsync(message, cancellationTokenSource.Token));
        }
    }
}
