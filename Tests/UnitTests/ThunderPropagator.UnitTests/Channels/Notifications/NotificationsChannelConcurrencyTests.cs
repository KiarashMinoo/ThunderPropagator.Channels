using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.BuildingBlocks.Application.Enums;
using ThunderPropagator.Channels.Notifications;
using ThunderPropagator.Channels.Notifications.Channel;
using ThunderPropagator.Channels.Notifications.Messages;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #78: NotificationsChannelBroadcastIsolationTests (#59) proved per-recipient message
    /// isolation for a single, sequential broadcast — each recipient gets its own instance, not a
    /// shared/mutated one. These tests extend that guarantee to concurrent emission: multiple
    /// broadcasts, and multiple targeted emissions, firing in parallel rather than one at a time,
    /// confirming the isolation holds under real concurrent load and that no recipient ever observes
    /// another recipient's or another message's content.
    /// </summary>
    public sealed class NotificationsChannelConcurrencyTests
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

        [Fact]
        public async Task ConcurrentTargetedEmissions_ToDifferentRecipients_NeverCrossContaminate()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            const int recipientCount = 25;
            var recipients = Enumerable.Range(0, recipientCount).Select(i => $"user-{i}").ToArray();

            await Task.WhenAll(recipients.Select(userId =>
                iChannel.EmitMessageAsync(
                    new NotificationsChannelFeederMessage { UserId = userId, Id = "notification-1", Subject = $"subject-for-{userId}" },
                    CancellationToken.None)));

            foreach (var userId in recipients)
            {
                var history = await channel.SearchHistoricalNotificationsAsync(userId);
                var entry = Assert.Single(history);
                Assert.Equal($"subject-for-{userId}", entry.Snapshot[nameof(NotificationsChannelFeederMessage.Subject)]);
                Assert.Equal(userId, entry.Snapshot[nameof(NotificationsChannelFeederMessage.UserId)]);
            }
        }

        [Fact]
        public async Task ConcurrentBroadcasts_WithDistinctContent_EachRecipientGetsAllOfThemUncorrupted()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            const int recipientCount = 10;
            const int broadcastCount = 10;

            // Seed known broadcast recipients first (fan-out discovery only finds users who already
            // have a stored CastType.Broadcast entry — see NotificationsChannelBroadcastIsolationTests).
            var recipients = Enumerable.Range(0, recipientCount).Select(i => $"member-{i}").ToArray();
            foreach (var userId in recipients)
            {
                await iChannel.EmitMessageAsync(
                    new NotificationsChannelFeederMessage { UserId = userId, CastType = CastType.Broadcast, Id = $"seed-{userId}", Subject = "subject" },
                    CancellationToken.None);
            }

            var broadcastIds = Enumerable.Range(0, broadcastCount).Select(i => $"broadcast-{i}").ToArray();

            await Task.WhenAll(broadcastIds.Select(id =>
                iChannel.EmitMessageAsync(
                    new NotificationsChannelFeederMessage { Id = id, Subject = $"subject-for-{id}", Audience = NotificationAudience.Broadcast },
                    CancellationToken.None)));

            foreach (var userId in recipients)
            {
                var history = await channel.SearchHistoricalNotificationsAsync(userId);
                var broadcastEntries = history.Where(entry => broadcastIds.Contains(entry.Snapshot[nameof(NotificationsChannelFeederMessage.Id)])).ToArray();

                Assert.Equal(broadcastCount, broadcastEntries.Length);
                foreach (var entry in broadcastEntries)
                {
                    var id = (string)entry.Snapshot[nameof(NotificationsChannelFeederMessage.Id)]!;
                    Assert.Equal($"subject-for-{id}", entry.Snapshot[nameof(NotificationsChannelFeederMessage.Subject)]);
                    Assert.Equal(userId, entry.Snapshot[nameof(NotificationsChannelFeederMessage.UserId)]);
                }
            }
        }

    }
}
