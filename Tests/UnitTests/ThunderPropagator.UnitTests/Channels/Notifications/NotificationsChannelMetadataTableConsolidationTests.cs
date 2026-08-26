using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Notifications;
using ThunderPropagator.Channels.Notifications.Channel;
using ThunderPropagator.Channels.Notifications.Messages;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #71: UserId and Date used to report their own field name as their schema table
    /// ("userId"/"date"), fragmenting schema discovery for fields that all belong to the same
    /// notification record — every other field already reported "notifications". All eighteen
    /// fields now share a single table, so metadata discovery returns one coherent schema.
    /// </summary>
    public sealed class NotificationsChannelMetadataTableConsolidationTests
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

        [Fact]
        public void AllEighteenFields_AreRegisteredUnderTheConsolidatedTable()
        {
            var channel = CreateChannel();

            var descriptors = channel.Metadata.ChannelProgramsDescriptors.ToArray();

            Assert.Equal(18, descriptors.Length);
            Assert.All(descriptors, descriptor => Assert.Equal("notifications", descriptor.Table));
        }

        [Fact]
        public void MetadataDiscovery_ReturnsExactlyOneCoherentTable()
        {
            var channel = CreateChannel();

            var tables = channel.Metadata.ChannelProgramsDescriptors
                .ToArray()
                .Select(descriptor => descriptor.Table)
                .Distinct()
                .ToArray();

            var table = Assert.Single(tables);
            Assert.Equal("notifications", table);
        }

        [Fact]
        public void UserId_IsUnderTheConsolidatedTable_NotItsOwnFieldName()
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(NotificationsChannelFeederMessage.UserId)];

            Assert.Equal("notifications", descriptor.Table);
        }

        [Fact]
        public void Date_IsUnderTheConsolidatedTable_NotItsOwnFieldName()
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(NotificationsChannelFeederMessage.Date)];

            Assert.Equal("notifications", descriptor.Table);
        }

        [Fact]
        public void UserId_RemainsTheOnlySubscribingKey_AfterConsolidation()
        {
            var channel = CreateChannel();

            var subscribingKeyNames = channel.Metadata.ChannelProgramsDescriptors.SubscribingKeys
                .Select(descriptor => descriptor.Name)
                .ToArray();

            Assert.Equal([nameof(NotificationsChannelFeederMessage.UserId)], subscribingKeyNames);
        }

        [Fact]
        public void Subscribe_SupplyingOnlyUserId_StillSucceedsAfterConsolidation()
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
    }
}
