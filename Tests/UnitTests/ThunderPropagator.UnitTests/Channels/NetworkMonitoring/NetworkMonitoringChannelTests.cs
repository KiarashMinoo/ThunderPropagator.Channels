using ThunderPropagator.Channels.NetworkMonitoring.Channel;
using ThunderPropagator.Channels.NetworkMonitoring.Configuration;
using ThunderPropagator.Channels.NetworkMonitoring.Feeders;
using ThunderPropagator.Channels.NetworkMonitoring.Messages;
using ThunderPropagator.Channels.NetworkMonitoring.Metadata;
﻿using Xunit;

namespace ThunderPropagator.UnitTests.Channels.NetworkMonitoring
{
    public class NetworkMonitoringChannelTests
    {
        [Fact]
        public void NetworkMonitoringChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.NetworkMonitoring.Channel.NetworkMonitoringChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void NetworkMonitoringChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.NetworkMonitoring.Configuration.NetworkMonitoringChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void NetworkMonitoringChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.NetworkMonitoring.Metadata.NetworkMonitoringChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void NetworkMonitoringChannelFeederMessage_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.NetworkMonitoring.Messages.NetworkMonitoringChannelFeederMessage);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void NetworkMonitoringChannelFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.NetworkMonitoring.Feeders.NetworkMonitoringChannelFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void NetworkMonitoringChannelFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.NetworkMonitoring.Feeders.NetworkMonitoringChannelFeederConfiguration);
            Assert.True(type.IsPublic);
        }
    }
}

