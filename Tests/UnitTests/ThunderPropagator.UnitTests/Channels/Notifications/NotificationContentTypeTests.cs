using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    public class NotificationContentTypeTests
    {
        [Fact]
        public void NotificationContentType_IsPublic()
        {
            var type = typeof(NotificationContentType);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void NotificationContentType_IsEnum()
        {
            var type = typeof(NotificationContentType);
            Assert.True(type.IsEnum);
        }

        [Theory]
        [InlineData(NotificationContentType.Text)]
        [InlineData(NotificationContentType.Html)]
        public void NotificationContentType_HasExpectedValues(NotificationContentType notificationContentType)
        {
            Assert.True(Enum.IsDefined(typeof(NotificationContentType), notificationContentType));
        }

        [Fact]
        public void NotificationContentType_HasTwoValues()
        {
            var values = Enum.GetValues<NotificationContentType>();
            Assert.Equal(2, values.Length);
        }
    }
}
