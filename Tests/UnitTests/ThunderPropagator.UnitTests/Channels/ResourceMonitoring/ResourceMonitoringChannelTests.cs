using ThunderPropagator.Channels.ResourceMonitoring.Channel;
using ThunderPropagator.Channels.ResourceMonitoring.Configuration;
using ThunderPropagator.Channels.ResourceMonitoring.Feeders;
using ThunderPropagator.Channels.ResourceMonitoring.Messages;
using ThunderPropagator.Channels.ResourceMonitoring.Metadata;
﻿using Xunit;

namespace ThunderPropagator.UnitTests.Channels.ResourceMonitoring
{
    public class ResourceMonitoringChannelTests
    {
        [Fact]
        public void ResourceMonitoringChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.ResourceMonitoring.Channel.ResourceMonitoringChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ResourceMonitoringChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.ResourceMonitoring.Configuration.ResourceMonitoringChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ResourceMonitoringChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.ResourceMonitoring.Metadata.ResourceMonitoringChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ResourceMonitoringChannelFeederMessage_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.ResourceMonitoring.Messages.ResourceMonitoringChannelFeederMessage);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ResourceMonitoringChannelFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.ResourceMonitoring.Feeders.ResourceMonitoringChannelFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ResourceMonitoringChannelFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.ResourceMonitoring.Feeders.ResourceMonitoringChannelFeederConfiguration);
            Assert.True(type.IsPublic);
        }
    }
}

