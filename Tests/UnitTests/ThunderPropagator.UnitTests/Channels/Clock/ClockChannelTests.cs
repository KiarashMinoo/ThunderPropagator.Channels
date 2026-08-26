using Xunit;
using ThunderPropagator.Channels.Clock.Channel;
using ThunderPropagator.Channels.Clock.Configuration;
using ThunderPropagator.Channels.Clock.Feeders;
using ThunderPropagator.Channels.Clock.Messages;
using ThunderPropagator.Channels.Clock.Metadata;

namespace ThunderPropagator.UnitTests.Channels.Clock
{
    public class ClockChannelTests
    {
        [Fact]
        public void ClockChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.Channel.ClockChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ClockChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.Configuration.ClockChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ClockChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.Metadata.ClockChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ClockChannelFeederMessage_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.Messages.ClockChannelFeederMessage);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void NowClockFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.Feeders.NowClockFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void UtcNowClockFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.Feeders.UtcNowClockFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void NowClockFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.Feeders.NowClockFeederConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void UtcNowClockFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Clock.Feeders.UtcNowClockFeederConfiguration);
            Assert.True(type.IsPublic);
        }
    }
}
