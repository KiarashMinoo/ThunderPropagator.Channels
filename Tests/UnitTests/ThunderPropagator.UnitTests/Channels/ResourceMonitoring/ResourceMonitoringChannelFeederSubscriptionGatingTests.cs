using NSubstitute;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.BuildingBlocks.Infrastructure.SystemResourceMonitor;
using ThunderPropagator.BuildingBlocks.Infrastructure.SystemResourceMonitor.Metrics.Cpu;
using ThunderPropagator.BuildingBlocks.Infrastructure.SystemResourceMonitor.Metrics.Memory;
using ThunderPropagator.Channels.ResourceMonitoring;
using ThunderPropagator.UnitTests.Feeders;
using ThunderPropagator.Channels.ResourceMonitoring.Channel;
using ThunderPropagator.Channels.ResourceMonitoring.Configuration;
using ThunderPropagator.Channels.ResourceMonitoring.Feeders;
using ThunderPropagator.Channels.ResourceMonitoring.Messages;

namespace ThunderPropagator.UnitTests.Channels.ResourceMonitoring
{
    public sealed class ResourceMonitoringChannelFeederSubscriptionGatingTests
    {
        private static (ResourceMonitoringChannelFeeder Feeder, ResourceMonitoringChannel Channel) CreateFeeder()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<ResourceMonitoringChannelFeederMessage, ResourceMonitoringChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new ResourceMonitoringChannelConfiguration());

            var resourceMonitor = Substitute.For<ISystemResourceMonitor>();
            resourceMonitor.GetMetricsAsync(Arg.Any<long?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
                .Returns(new SystemResourceMonitorMetrics
                {
                    Cpu = new CpuMetrics(4, 10.0, 8, 20, 100),
                    Memory = new MemoryMetrics(1000, 500)
                });
            serviceProvider.RegisterService(resourceMonitor);

            var channel = new ResourceMonitoringChannel(serviceProvider);

            // Keep the poll interval short so the test doesn't wait the 1-second default.
            var feederConfiguration = new ResourceMonitoringChannelFeederConfiguration { UtilizationWindow = 1 };
            var feederHandler = new NoOpFeederHandler<ResourceMonitoringChannel, ResourceMonitoringChannelFeederMessage>();

            var feeder = new ResourceMonitoringChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);
            return (feeder, channel);
        }

        private static async Task<int> CountEmittedAsync(ResourceMonitoringChannelFeeder feeder)
        {
            var count = 0;
            await foreach (var _ in FeederCancellationTestHelper.InvokeReceiveAsync<ResourceMonitoringChannelFeederMessage>(feeder, CancellationToken.None))
                count++;
            return count;
        }

        [Fact]
        public async Task ReceiveAsync_NoActiveSubscriptions_YieldsNoMessages()
        {
            var (feeder, _) = CreateFeeder();

            Assert.Equal(0, await CountEmittedAsync(feeder));
        }

        [Fact]
        public async Task ReceiveAsync_AfterSubscriptionAdded_YieldsMessage()
        {
            var (feeder, channel) = CreateFeeder();

            ChannelSubscriptionTestHelper.RaiseSubscriptionAdded(channel);

            Assert.Equal(1, await CountEmittedAsync(feeder));
        }

        [Fact]
        public async Task ReceiveAsync_AfterSubscribeThenUnsubscribe_YieldsNoMessages()
        {
            var (feeder, channel) = CreateFeeder();

            ChannelSubscriptionTestHelper.RaiseSubscriptionAdded(channel);
            ChannelSubscriptionTestHelper.RaiseSubscriptionRemoved(channel);

            Assert.Equal(0, await CountEmittedAsync(feeder));
        }
    }
}
