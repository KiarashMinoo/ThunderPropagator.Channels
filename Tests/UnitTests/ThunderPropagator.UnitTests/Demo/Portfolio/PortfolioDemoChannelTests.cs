﻿using Xunit;

namespace ThunderPropagator.UnitTests.Demo.Portfolio
{
    public class PortfolioDemoChannelTests
    {
        [Fact]
        public void PortfolioDemoChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.PortfolioDemoChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PortfolioDemoChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.PortfolioDemoChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PortfolioDemoChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.PortfolioDemoChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PortfolioDemoChannelFeederMessage_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.PortfolioDemoChannelFeederMessage);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PortfolioDemoChannelBuyReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Pipelines.PortfolioDemoChannelBuyReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PortfolioDemoChannelSellReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Pipelines.PortfolioDemoChannelSellReceiverPipeline);
            Assert.True(type.IsPublic);
        }
    }
}

