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
        public void PortfolioDemoChannelFeederMessage_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.PortfolioDemoChannelFeederMessage);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void PortfolioDemoChannelBuyReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Pipelines.PortfolioDemoChannelBuyReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void PortfolioDemoChannelSellReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Pipelines.PortfolioDemoChannelSellReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void PortfolioDemoChannelConfiguration_PollIntervalBounds_HaveNamedNonZeroDefaults()
        {
            var configuration = new ThunderPropagator.Channels.Demo.Portfolio.PortfolioDemoChannelConfiguration();

            Assert.Equal(TimeSpan.FromMilliseconds(500), configuration.MinPollInterval);
            Assert.Equal(TimeSpan.FromSeconds(90), configuration.MaxPollInterval);
            Assert.True(configuration.MinPollInterval < configuration.MaxPollInterval);
        }
    }
}

