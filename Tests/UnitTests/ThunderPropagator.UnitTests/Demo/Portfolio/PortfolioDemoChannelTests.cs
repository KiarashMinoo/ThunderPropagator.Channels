using ThunderPropagator.Channels.Demo.Portfolio.Channel;
using ThunderPropagator.Channels.Demo.Portfolio.Configuration;
using ThunderPropagator.Channels.Demo.Portfolio.Messages;
using ThunderPropagator.Channels.Demo.Portfolio.Metadata;
﻿using Xunit;

namespace ThunderPropagator.UnitTests.Demo.Portfolio
{
    public class PortfolioDemoChannelTests
    {
        [Fact]
        public void PortfolioDemoChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Channel.PortfolioDemoChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PortfolioDemoChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Configuration.PortfolioDemoChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PortfolioDemoChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Metadata.PortfolioDemoChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PortfolioDemoChannelFeederMessage_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Messages.PortfolioDemoChannelFeederMessage);
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
            var configuration = new ThunderPropagator.Channels.Demo.Portfolio.Configuration.PortfolioDemoChannelConfiguration();

            Assert.Equal(TimeSpan.FromMilliseconds(500), configuration.MinPollInterval);
            Assert.Equal(TimeSpan.FromSeconds(90), configuration.MaxPollInterval);
            Assert.True(configuration.MinPollInterval < configuration.MaxPollInterval);
        }
    }
}

