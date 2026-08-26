using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Channels.Notifications;
using ThunderPropagator.Channels.Notifications.Channel;
using ThunderPropagator.Channels.Notifications.Messages;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #70: Seen used to be a raw int with an undocumented bitwise convention, letting callers
    /// create meaningless combinations with no compiler or runtime guidance. Seen is now typed
    /// NotificationDeliveryState, a [Flags] enum with stable explicit numeric values; the setter
    /// rejects a value with any bit outside those flags, and the getter still reads a legacy raw
    /// int previously stored under the same field name correctly, since a [Flags] enum's underlying
    /// representation is an int.
    /// </summary>
    public sealed class NotificationDeliveryStateTests
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

        [Theory]
        [InlineData(NotificationDeliveryState.None, 0)]
        [InlineData(NotificationDeliveryState.Delivered, 1)]
        [InlineData(NotificationDeliveryState.Seen, 2)]
        [InlineData(NotificationDeliveryState.Read, 4)]
        [InlineData(NotificationDeliveryState.Dismissed, 8)]
        public void EachFlag_HasItsDocumentedStableNumericValue(NotificationDeliveryState flag, int expectedValue)
        {
            Assert.Equal(expectedValue, (int)flag);
        }

        [Fact]
        public void EnumType_IsDecoratedWithFlagsAttribute()
        {
            Assert.True(Attribute.IsDefined(typeof(NotificationDeliveryState), typeof(FlagsAttribute)));
        }

        [Fact]
        public void Seen_DefaultsToNone()
        {
            var message = new NotificationsChannelFeederMessage();

            Assert.Equal(NotificationDeliveryState.None, message.Seen);
        }

        [Theory]
        [InlineData(NotificationDeliveryState.Delivered)]
        [InlineData(NotificationDeliveryState.Seen)]
        [InlineData(NotificationDeliveryState.Read)]
        [InlineData(NotificationDeliveryState.Dismissed)]
        public void Seen_AcceptsEachIndividualFlag(NotificationDeliveryState flag)
        {
            var message = new NotificationsChannelFeederMessage { Seen = flag };

            Assert.Equal(flag, message.Seen);
        }

        [Theory]
        [InlineData(NotificationDeliveryState.Delivered | NotificationDeliveryState.Seen)]
        [InlineData(NotificationDeliveryState.Delivered | NotificationDeliveryState.Seen | NotificationDeliveryState.Read)]
        [InlineData(NotificationDeliveryState.Delivered | NotificationDeliveryState.Seen | NotificationDeliveryState.Read | NotificationDeliveryState.Dismissed)]
        public void Seen_AcceptsCombinationsOfDefinedFlags(NotificationDeliveryState combination)
        {
            var message = new NotificationsChannelFeederMessage { Seen = combination };

            Assert.Equal(combination, message.Seen);
        }

        [Fact]
        public void Seen_RejectsAValueWithBitsOutsideTheDefinedFlags()
        {
            var exception = Assert.Throws<NotificationsChannelFeederMessageValidationException>(
                () => new NotificationsChannelFeederMessage { Seen = (NotificationDeliveryState)99 });

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Seen), exception.PropertyName);
        }

        [Fact]
        public void DictionaryConstruction_WithALegacyRawIntValue_IsReadAsTheEquivalentFlags()
        {
            IDictionary<string, object?> raw = new Dictionary<string, object?>
            {
                [nameof(NotificationsChannelFeederMessage.Id)] = "notification-1",
                [nameof(NotificationsChannelFeederMessage.Subject)] = "subject",
                // Simulates a message constructed by an earlier version of this package, before Seen
                // was flags-typed, where a raw int was stored under the same field name.
                [nameof(NotificationsChannelFeederMessage.Seen)] = 3
            };

            var message = new NotificationsChannelFeederMessage(raw);

            Assert.Equal(NotificationDeliveryState.Delivered | NotificationDeliveryState.Seen, message.Seen);
        }

        [Theory]
        [InlineData(NotificationDeliveryState.None)]
        [InlineData(NotificationDeliveryState.Delivered)]
        [InlineData(NotificationDeliveryState.Seen)]
        [InlineData(NotificationDeliveryState.Read)]
        [InlineData(NotificationDeliveryState.Dismissed)]
        [InlineData(NotificationDeliveryState.Delivered | NotificationDeliveryState.Seen | NotificationDeliveryState.Read | NotificationDeliveryState.Dismissed)]
        public void DictionaryConstruction_RoundTripsEveryFlagAndCombination(NotificationDeliveryState value)
        {
            IDictionary<string, object?> raw = new Dictionary<string, object?>
            {
                [nameof(NotificationsChannelFeederMessage.Id)] = "notification-1",
                [nameof(NotificationsChannelFeederMessage.Subject)] = "subject",
                [nameof(NotificationsChannelFeederMessage.Seen)] = value
            };

            var message = new NotificationsChannelFeederMessage(raw);

            Assert.Equal(value, message.Seen);
        }

        [Fact]
        public void CopyConstructor_PreservesACombinationOfFlags()
        {
            var source = new NotificationsChannelFeederMessage
            {
                Id = "notification-1",
                Subject = "subject",
                Seen = NotificationDeliveryState.Delivered | NotificationDeliveryState.Seen
            };

            var copy = new NotificationsChannelFeederMessage(source);

            Assert.Equal(NotificationDeliveryState.Delivered | NotificationDeliveryState.Seen, copy.Seen);
        }

        [Fact]
        public void ChannelMetadata_DeclaresSeenAsAnEnumDescriptorOfNotificationDeliveryState()
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(NotificationsChannelFeederMessage.Seen)];

            Assert.IsType<EnumChannelProgramsDescriptor<NotificationDeliveryState>>(descriptor);
        }
    }
}
