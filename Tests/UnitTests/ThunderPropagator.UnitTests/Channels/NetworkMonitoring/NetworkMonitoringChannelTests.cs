﻿using Xunit;

namespace ThunderPropagator.UnitTests.Channels.NetworkMonitoring
{
    public class NetworkMonitoringChannelTests
    {
        [Fact]
        public void NetworkMonitoringChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.NetworkMonitoring.NetworkMonitoringChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void NetworkMonitoringChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.NetworkMonitoring.NetworkMonitoringChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void NetworkMonitoringChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.NetworkMonitoring.NetworkMonitoringChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void NetworkMonitoringChannelFeederMessage_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.NetworkMonitoring.NetworkMonitoringChannelFeederMessage);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void NetworkMonitoringChannelFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.NetworkMonitoring.NetworkMonitoringChannelFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void NetworkMonitoringChannelFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.NetworkMonitoring.NetworkMonitoringChannelFeederConfiguration);
            Assert.True(type.IsPublic);
        }
    }
}

