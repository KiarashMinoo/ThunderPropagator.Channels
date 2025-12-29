﻿using Xunit;

namespace ThunderPropagator.UnitTests.Demo.Airport
{
    public class AirportDemoChannelTests
    {
        [Fact]
        public void AirportDemoChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Airport.AirportDemoChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void AirportDemoChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Airport.AirportDemoChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void AirportDemoChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Airport.AirportDemoChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void AirportDemoChannelFeederMessage_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Airport.AirportDemoChannelFeederMessage);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void AirportDemoChannelFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Airport.AirportDemoChannelFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void AirportDemoChannelFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Airport.AirportDemoChannelFeederConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void Statuses_IsEnum()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Airport.Statuses);
            Assert.True(type.IsEnum);
        }
    }
}

