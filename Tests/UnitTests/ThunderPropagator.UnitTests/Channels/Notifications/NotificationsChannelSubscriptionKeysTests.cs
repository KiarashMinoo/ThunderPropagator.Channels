using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.BuildingBlocks.Application.Enums;
using ThunderPropagator.Channels.Notifications;
using ThunderPropagator.Channels.Notifications.Channel;
using ThunderPropagator.Channels.Notifications.Messages;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #61: Date used to be registered alongside UserId as a SubscribingKeyChannelProgramsDescriptor,
    /// which put it in the channel's declared subscription-key contract even though nothing about a
    /// live notification subscription depends on a date. These tests confirm UserId is now the sole
    /// subscribing key, that a client can subscribe supplying only UserId through the real public
    /// Subscribe() entry point, and that the new explicit (UserId, Id) snapshot hash keeps multiple
    /// notifications for the same user as distinct history entries — Date being dropped from the
    /// subscribing-key set would otherwise have collapsed every notification for a user into a
    /// single overwritten snapshot slot, since the framework derives its snapshot hash from
    /// whatever fields are declared as subscribing keys.
    /// </summary>
    public sealed class NotificationsChannelSubscriptionKeysTests
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

        private static void SetDate(NotificationsChannelFeederMessage message, DateTime date)
            => typeof(NotificationsChannelFeederMessage).GetProperty(nameof(NotificationsChannelFeederMessage.Date))!.GetSetMethod(true)!.Invoke(message, [date]);

        [Fact]
        public void SubscribingKeys_ContainsOnlyUserId()
        {
            var channel = CreateChannel();

            var subscribingKeyNames = channel.Metadata.ChannelProgramsDescriptors.SubscribingKeys
                .Select(descriptor => descriptor.Name)
                .ToArray();

            Assert.Equal([nameof(NotificationsChannelFeederMessage.UserId)], subscribingKeyNames);
        }

        [Fact]
        public void Date_IsStillADeclaredField_JustNotASubscribingKey()
        {
            var channel = CreateChannel();

            var dateDescriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(NotificationsChannelFeederMessage.Date)];

            Assert.IsType<DateTimeChannelProgramsDescriptor>(dateDescriptor);
            Assert.IsNotType<SubscribingKeyChannelProgramsDescriptor>(dateDescriptor);
        }

        [Fact]
        public void Subscribe_SupplyingOnlyUserId_Succeeds()
        {
            var channel = CreateChannel();

            var subscribeRequest = Substitute.For<ISubscribeRequest>();
            subscribeRequest.SubscribingKeys.Returns(new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["subscription-1"] = new Dictionary<string, string> { [nameof(NotificationsChannelFeederMessage.UserId)] = "user-1" }
            });
            subscribeRequest.SubscribingFields.Returns(Array.Empty<string>());

            var exception = Record.Exception(() => channel.Subscribe(Substitute.For<IConnectionInfo>(), "request-1", subscribeRequest));

            Assert.Null(exception);
        }

        [Fact]
        public async Task MultipleNotificationsForSameUser_AreStoredAsDistinctHistoryEntries()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject" }, CancellationToken.None);
            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-2", Subject = "subject" }, CancellationToken.None);

            var history = await channel.SearchHistoricalNotificationsAsync("user-1");

            Assert.Equal(2, history.Length);
        }

        [Fact]
        public async Task SearchHistoricalNotificationsAsync_WithNoFilter_ReturnsFullHistory()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject" }, CancellationToken.None);
            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-2", Subject = "subject" }, CancellationToken.None);

            var history = await channel.SearchHistoricalNotificationsAsync("user-1", dateRange: null);

            Assert.Equal(2, history.Length);
        }

        [Fact]
        public async Task SearchHistoricalNotificationsAsync_DoesNotReturnOtherUsersNotifications()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject" }, CancellationToken.None);
            await iChannel.EmitMessageAsync(new NotificationsChannelFeederMessage { UserId = "user-2", Id = "notification-2", Subject = "subject" }, CancellationToken.None);

            var history = await channel.SearchHistoricalNotificationsAsync("user-1");

            var entry = Assert.Single(history);
            Assert.Equal("notification-1", entry.Snapshot[nameof(NotificationsChannelFeederMessage.Id)]);
        }

        [Fact]
        public async Task SearchHistoricalNotificationsAsync_WithDateRange_ExcludesEntriesOutsideTheRange()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            EnableSnapshotting(channel);

            var inRange = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-in-range", Subject = "subject" };
            SetDate(inRange, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

            var outOfRange = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-out-of-range", Subject = "subject" };
            SetDate(outOfRange, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

            await iChannel.EmitMessageAsync(inRange, CancellationToken.None);
            await iChannel.EmitMessageAsync(outOfRange, CancellationToken.None);

            var dateRange = new NotificationsHistoricalDateRangeFilter(
                from: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                to: new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc));

            var history = await channel.SearchHistoricalNotificationsAsync("user-1", dateRange);

            var entry = Assert.Single(history);
            Assert.Equal("notification-in-range", entry.Snapshot[nameof(NotificationsChannelFeederMessage.Id)]);
        }
    }
}
