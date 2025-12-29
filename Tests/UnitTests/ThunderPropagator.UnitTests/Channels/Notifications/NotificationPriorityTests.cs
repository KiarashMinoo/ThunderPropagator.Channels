using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    public class NotificationPriorityTests
    {
        [Fact]
        public void NotificationPriority_IsPublic()
        {
            var type = typeof(NotificationPriority);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void NotificationPriority_IsEnum()
        {
            var type = typeof(NotificationPriority);
            Assert.True(type.IsEnum);
        }

        [Theory]
        [InlineData(NotificationPriority.VeryLow, -2)]
        [InlineData(NotificationPriority.Low, -1)]
        [InlineData(NotificationPriority.Normal, 0)]
        [InlineData(NotificationPriority.High, 1)]
        [InlineData(NotificationPriority.VeryHigh, 2)]
        public void NotificationPriority_HasExpectedValues(NotificationPriority priority, int expectedValue)
        {
            Assert.Equal(expectedValue, (int)priority);
        }

        [Fact]
        public void NotificationPriority_HasFiveValues()
        {
            var values = Enum.GetValues<NotificationPriority>();
            Assert.Equal(5, values.Length);
        }

        [Fact]
        public void NotificationPriority_VeryLowIsLowest()
        {
            Assert.True(NotificationPriority.VeryLow < NotificationPriority.Low);
            Assert.True(NotificationPriority.VeryLow < NotificationPriority.Normal);
        }

        [Fact]
        public void NotificationPriority_VeryHighIsHighest()
        {
            Assert.True(NotificationPriority.VeryHigh > NotificationPriority.High);
            Assert.True(NotificationPriority.VeryHigh > NotificationPriority.Normal);
        }
    }
}
