using Xunit;
using ThunderPropagator.Channels.Notifications;
using ThunderPropagator.Channels.Notifications.Channel;
using ThunderPropagator.Channels.Notifications.Feeders;
using ThunderPropagator.Channels.Notifications.Messages;
using ThunderPropagator.Channels.Notifications.Metadata;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    public class NotificationsChannelTests
    {
        [Fact]
        public void NotificationsChannel_IsGeneric()
        {
            var type = typeof(NotificationsChannel<>);
            Assert.True(type.IsGenericTypeDefinition);
        }

        [Fact]
        public void NotificationsChannelMetadata_IsGeneric()
        {
            var type = typeof(NotificationsChannelMetadata<>);
            Assert.True(type.IsGenericTypeDefinition);
        }

        [Fact]
        public void NotificationsChannelFeederMessage_IsInternal()
        {
            var type = typeof(NotificationsChannelFeederMessage);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void NotificationContentType_IsEnum()
        {
            var type = typeof(NotificationContentType);
            Assert.True(type.IsEnum);
        }

        [Fact]
        public void NotificationCategory_IsEnum()
        {
            var type = typeof(NotificationCategory);
            Assert.True(type.IsEnum);
        }

        [Fact]
        public void NotificationPriority_IsEnum()
        {
            var type = typeof(NotificationPriority);
            Assert.True(type.IsEnum);
        }

        [Fact]
        public void NotificationsFeederConfiguration_IsPublic()
        {
            var type = typeof(NotificationsFeederConfiguration);
            Assert.True(type.IsPublic);
        }
    }
}

