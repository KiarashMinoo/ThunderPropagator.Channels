using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    public class NotificationTypeTests
    {
        [Fact]
        public void NotificationType_IsPublic()
        {
            var type = typeof(NotificationType);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void NotificationType_IsEnum()
        {
            var type = typeof(NotificationType);
            Assert.True(type.IsEnum);
        }

        [Theory]
        [InlineData(NotificationType.Text)]
        [InlineData(NotificationType.Html)]
        public void NotificationType_HasExpectedValues(NotificationType notificationType)
        {
            Assert.True(Enum.IsDefined(typeof(NotificationType), notificationType));
        }

        [Fact]
        public void NotificationType_HasTwoValues()
        {
            var values = Enum.GetValues<NotificationType>();
            Assert.Equal(2, values.Length);
        }
    }
}
