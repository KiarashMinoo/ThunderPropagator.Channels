using ThunderPropagator.Channels.Demo.Airport.Channel;
using ThunderPropagator.Channels.Demo.Airport.Configuration;
using ThunderPropagator.Channels.Demo.Airport.Feeders;
using ThunderPropagator.Channels.Demo.Airport.Messages;
using ThunderPropagator.Channels.Demo.Airport.Metadata;
﻿using Xunit;

namespace ThunderPropagator.UnitTests.Demo.Airport
{
    public class AirportDemoChannelTests
    {
        [Fact]
        public void AirportDemoChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Airport.Channel.AirportDemoChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void AirportDemoChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Airport.Configuration.AirportDemoChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void AirportDemoChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Airport.Metadata.AirportDemoChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void AirportDemoChannelFeederMessage_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Airport.Messages.AirportDemoChannelFeederMessage);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void AirportDemoChannelFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Airport.Feeders.AirportDemoChannelFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void AirportDemoChannelFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Airport.Feeders.AirportDemoChannelFeederConfiguration);
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

