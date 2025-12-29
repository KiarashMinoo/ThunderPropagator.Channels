using Xunit;

namespace ThunderPropagator.UnitTests.Channels.Clock
{
    public class ClockChannelTests
    {
        [Fact]
        public void ClockChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.ClockChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ClockChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.ClockChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ClockChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.ClockChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ClockChannelFeederMessage_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.ClockChannelFeederMessage);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void NowClockFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.NowClockFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void UtcNowClockFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.UtcNowClockFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void NowClockFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.NowClockFeederConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void UtcNowClockFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.UtcNowClockFeederConfiguration);
            Assert.True(type.IsPublic);
        }
    }
}
