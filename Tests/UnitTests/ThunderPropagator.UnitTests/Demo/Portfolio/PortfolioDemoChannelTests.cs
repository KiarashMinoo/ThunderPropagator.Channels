using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Channels.Demo.Portfolio.Channel;
using ThunderPropagator.Channels.Demo.Portfolio.Configuration;
using ThunderPropagator.Channels.Demo.Portfolio.Messages;
using ThunderPropagator.Channels.Demo.Portfolio.Metadata;
﻿using Xunit;

namespace ThunderPropagator.UnitTests.Demo.Portfolio
{
    public class PortfolioDemoChannelTests
    {
        private static PortfolioDemoChannel CreateChannel()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(ILogger<PortfolioDemoChannel>)).Returns(NullLogger<PortfolioDemoChannel>.Instance);
            serviceProvider.GetService(typeof(PortfolioDemoChannelConfiguration)).Returns(new PortfolioDemoChannelConfiguration());

            var channel = new PortfolioDemoChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return channel;
        }

        // Issue #36: FindSubscribedKey is what Buy/Sell now use instead of trusting a caller-supplied
        // Key. A connection with no active subscription (never subscribed, or already unsubscribed)
        // must get null back, not some stale or default Key that could accidentally resolve to a real
        // portfolio entry. The "found" path (a real subscription actually mapping to its own Key)
        // isn't covered here: constructing a real Subscription needs the framework's own internal
        // subscribe pipeline, whose constructor this project has no access to — the same limitation
        // RockPaperScissorsComputerTests already documents for that module's own subscription-dependent
        // paths.
        [Fact]
        public void FindSubscribedKey_ForAConnectionWithNoActiveSubscription_ReturnsNull()
        {
            var channel = CreateChannel();

            var key = channel.FindSubscribedKey("unknown-connection");

            Assert.Null(key);
        }

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

