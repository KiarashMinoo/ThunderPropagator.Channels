﻿using Xunit;

namespace ThunderPropagator.UnitTests.Channels.Throughput
{
    public class ThroughputChannelTests
    {
        [Fact]
        public void ThroughputChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Throughput.ThroughputChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ThroughputChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Throughput.ThroughputChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ThroughputChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Throughput.ThroughputChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ThroughputChannelFeederMessage_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Throughput.ThroughputChannelFeederMessage);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ThroughputChannelFeeder_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Throughput.ThroughputChannelFeeder);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ThroughputChannelFeederConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Throughput.ThroughputChannelFeederConfiguration);
            Assert.True(type.IsPublic);
        }
    }
}

