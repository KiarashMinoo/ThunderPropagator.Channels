using ThunderPropagator.Channels.Demo.StockListBasic.Channel;
using ThunderPropagator.Channels.Demo.StockListBasic.Configuration;
using ThunderPropagator.Channels.Demo.StockListBasic.Feeders;
using ThunderPropagator.Channels.Demo.StockListBasic.Messages;
using ThunderPropagator.Channels.Demo.StockListBasic.Metadata;
﻿using Xunit;

namespace ThunderPropagator.UnitTests.Demo.StockListBasic
{
    public class StockListBasicDemoChannelTests
    {
        [Fact]
        public void StockListBasicDemoChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.StockListBasic.Channel.StockListBasicDemoChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void StockListBasicDemoChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.StockListBasic.Configuration.StockListBasicDemoChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void StockListBasicDemoChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.StockListBasic.Metadata.StockListBasicDemoChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void StockListBasicDemoChannelFeederMessage_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.StockListBasic.Messages.StockListBasicDemoChannelFeederMessage);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void StockListBasicDemoChannelFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.StockListBasic.Feeders.StockListBasicDemoChannelFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void StockListBasicDemoChannelFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.StockListBasic.Feeders.StockListBasicDemoChannelFeederConfiguration);
            Assert.True(type.IsPublic);
        }
    }
}

