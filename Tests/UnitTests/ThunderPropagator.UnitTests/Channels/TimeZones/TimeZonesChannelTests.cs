using ThunderPropagator.Channels.TimeZones.Channel;
using ThunderPropagator.Channels.TimeZones.Configuration;
using ThunderPropagator.Channels.TimeZones.Feeders;
using ThunderPropagator.Channels.TimeZones.Messages;
using ThunderPropagator.Channels.TimeZones.Metadata;
﻿using Xunit;

namespace ThunderPropagator.UnitTests.Channels.TimeZones
{
    public class TimeZonesChannelTests
    {
        [Fact]
        public void TimeZonesChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.TimeZones.Channel.TimeZonesChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void TimeZonesChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.TimeZones.Configuration.TimeZonesChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void TimeZonesChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.TimeZones.Metadata.TimeZonesChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void TimeZonesChannelFeederMessage_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.TimeZones.Messages.TimeZonesChannelFeederMessage);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void TimeZonesChannelFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.TimeZones.Feeders.TimeZonesChannelFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void TimeZonesChannelFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.TimeZones.Feeders.TimeZonesChannelFeederConfiguration);
            Assert.True(type.IsPublic);
        }
    }
}

