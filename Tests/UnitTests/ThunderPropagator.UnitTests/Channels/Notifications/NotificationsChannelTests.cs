using Xunit;
using ThunderPropagator.Channels.Notifications;

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
        public void NotificationsChannelFeederMessage_IsPublic()
        {
            var type = typeof(NotificationsChannelFeederMessage);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void NotificationType_IsEnum()
        {
            var type = typeof(NotificationType);
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

