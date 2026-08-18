using System.Runtime.CompilerServices;
using ThunderPropagator.BuildingBlocks.Application.Enums;
using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #60: Origin, Type, Priority, Icon, Subject, Body, EllipsisBody, and Metadata used to be
    /// private set, so a consumer outside this assembly had no way to populate them through normal
    /// object initialization. These tests exercise the object-initializer syntax a package consumer
    /// would actually write, and confirm the setters are compiler-enforced init-only rather than
    /// merely public by convention.
    /// </summary>
    public sealed class NotificationsChannelFeederMessageConstructionTests
    {
        [Fact]
        public void ExternalConsumer_CanFullyPopulateAMessage_ThroughObjectInitialization()
        {
            var message = new NotificationsChannelFeederMessage
            {
                UserId = "user-1",
                Id = "notification-1",
                Origin = "billing-service",
                Type = NotificationContentType.Html,
                Category = NotificationCategory.Warning,
                Priority = NotificationPriority.High,
                Icon = "bell",
                Subject = "Invoice ready",
                Body = "Your invoice is ready to view.",
                EllipsisBody = "Your invoice is ready…",
                Seen = NotificationDeliveryState.Delivered,
                Metadata = "{\"invoiceId\":42}",
                CastType = CastType.Broadcast,
                IsDeleted = false,
                CorrelationId = "correlation-1"
            };

            Assert.Equal("user-1", message.UserId);
            Assert.Equal("notification-1", message.Id);
            Assert.Equal("billing-service", message.Origin);
            Assert.Equal(NotificationContentType.Html, message.Type);
            Assert.Equal(NotificationCategory.Warning, message.Category);
            Assert.Equal(NotificationPriority.High, message.Priority);
            Assert.Equal("bell", message.Icon);
            Assert.Equal("Invoice ready", message.Subject);
            Assert.Equal("Your invoice is ready to view.", message.Body);
            Assert.Equal("Your invoice is ready…", message.EllipsisBody);
            Assert.Equal(NotificationDeliveryState.Delivered, message.Seen);
            Assert.Equal("{\"invoiceId\":42}", message.Metadata);
            Assert.Equal(CastType.Broadcast, message.CastType);
            Assert.False(message.IsDeleted);
            Assert.Equal("correlation-1", message.CorrelationId);
        }

        [Theory]
        [InlineData(nameof(NotificationsChannelFeederMessage.Origin))]
        [InlineData(nameof(NotificationsChannelFeederMessage.Type))]
        [InlineData(nameof(NotificationsChannelFeederMessage.Category))]
        [InlineData(nameof(NotificationsChannelFeederMessage.Priority))]
        [InlineData(nameof(NotificationsChannelFeederMessage.Icon))]
        [InlineData(nameof(NotificationsChannelFeederMessage.Subject))]
        [InlineData(nameof(NotificationsChannelFeederMessage.Body))]
        [InlineData(nameof(NotificationsChannelFeederMessage.EllipsisBody))]
        [InlineData(nameof(NotificationsChannelFeederMessage.Metadata))]
        public void Property_IsInitOnly_SoItCannotBeReassignedAfterConstruction(string propertyName)
        {
            var setMethod = typeof(NotificationsChannelFeederMessage).GetProperty(propertyName)!.GetSetMethod(nonPublic: true)!;

            // init accessors are plain `set` methods tagged with an IsExternalInit required custom
            // modifier on the return type — this is the same signal the C# compiler itself checks
            // to reject `message.Origin = ...` outside object initialization. Asserting it directly
            // (rather than just calling the setter and expecting it to work) is what actually
            // catches a regression back to a plain public `set`, which would compile fine but
            // silently drop the immutability guarantee this issue asks for.
            Assert.Contains(typeof(IsExternalInit), setMethod.ReturnParameter.GetRequiredCustomModifiers());
        }

        [Fact]
        public void DictionaryConstruction_StillPopulatesTheSameFields()
        {
            IDictionary<string, object?> raw = new Dictionary<string, object?>
            {
                [nameof(NotificationsChannelFeederMessage.UserId)] = "user-1",
                [nameof(NotificationsChannelFeederMessage.Id)] = "notification-1",
                [nameof(NotificationsChannelFeederMessage.Origin)] = "billing-service",
                [nameof(NotificationsChannelFeederMessage.Type)] = NotificationContentType.Html,
                [nameof(NotificationsChannelFeederMessage.Subject)] = "Invoice ready"
            };

            var message = new NotificationsChannelFeederMessage(raw);

            Assert.Equal("user-1", message.UserId);
            Assert.Equal("billing-service", message.Origin);
            Assert.Equal(NotificationContentType.Html, message.Type);
            Assert.Equal("Invoice ready", message.Subject);
        }
    }
}
