using ThunderPropagator.Channels.Throughput.Channel;
using ThunderPropagator.Channels.Throughput.Configuration;
using ThunderPropagator.Channels.Throughput.Feeders;
using ThunderPropagator.Channels.Throughput.Messages;
using ThunderPropagator.Channels.Throughput.Metadata;
﻿using Xunit;

namespace ThunderPropagator.UnitTests.Channels.Throughput
{
    public class ThroughputChannelTests
    {
        [Fact]
        public void ThroughputChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Throughput.Channel.ThroughputChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ThroughputChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Throughput.Configuration.ThroughputChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ThroughputChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Throughput.Metadata.ThroughputChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ThroughputChannelFeederMessage_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Throughput.Messages.ThroughputChannelFeederMessage);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ThroughputChannelFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Throughput.Feeders.ThroughputChannelFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ThroughputChannelFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Throughput.Feeders.ThroughputChannelFeederConfiguration);
            Assert.True(type.IsPublic);
        }
    }
}

