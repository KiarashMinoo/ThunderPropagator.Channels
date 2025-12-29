﻿using Xunit;

namespace ThunderPropagator.UnitTests.Channels.ResourceMonitoring
{
    public class ResourceMonitoringChannelTests
    {
        [Fact]
        public void ResourceMonitoringChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.ResourceMonitoring.ResourceMonitoringChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ResourceMonitoringChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.ResourceMonitoring.ResourceMonitoringChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ResourceMonitoringChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.ResourceMonitoring.ResourceMonitoringChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ResourceMonitoringChannelFeederMessage_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.ResourceMonitoring.ResourceMonitoringChannelFeederMessage);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ResourceMonitoringChannelFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.ResourceMonitoring.ResourceMonitoringChannelFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ResourceMonitoringChannelFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.ResourceMonitoring.ResourceMonitoringChannelFeederConfiguration);
            Assert.True(type.IsPublic);
        }
    }
}

