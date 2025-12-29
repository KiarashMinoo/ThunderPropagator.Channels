﻿using Xunit;

namespace ThunderPropagator.UnitTests.Demo.StockListBasic
{
    public class StockListBasicDemoChannelTests
    {
        [Fact]
        public void StockListBasicDemoChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.StockListBasic.StockListBasicDemoChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void StockListBasicDemoChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.StockListBasic.StockListBasicDemoChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void StockListBasicDemoChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.StockListBasic.StockListBasicDemoChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void StockListBasicDemoChannelFeederMessage_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.StockListBasic.StockListBasicDemoChannelFeederMessage);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void StockListBasicDemoChannelFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.StockListBasic.StockListBasicDemoChannelFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void StockListBasicDemoChannelFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.StockListBasic.StockListBasicDemoChannelFeederConfiguration);
            Assert.True(type.IsPublic);
        }
    }
}

