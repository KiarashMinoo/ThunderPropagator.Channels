﻿using Xunit;

namespace ThunderPropagator.UnitTests.Channels.TimeZones
{
    public class TimeZonesChannelTests
    {
        [Fact]
        public void TimeZonesChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.TimeZones.TimeZonesChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void TimeZonesChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.TimeZones.TimeZonesChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void TimeZonesChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.TimeZones.TimeZonesChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void TimeZonesChannelFeederMessage_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.TimeZones.TimeZonesChannelFeederMessage);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void TimeZonesChannelFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.TimeZones.TimeZonesChannelFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void TimeZonesChannelFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.TimeZones.TimeZonesChannelFeederConfiguration);
            Assert.True(type.IsPublic);
        }
    }
}

