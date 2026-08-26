using NSubstitute;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.BuildingBlocks.Infrastructure.SystemResourceMonitor;
using ThunderPropagator.Channels.ResourceMonitoring;
using ThunderPropagator.UnitTests.Feeders;
using ThunderPropagator.Channels.ResourceMonitoring.Channel;
using ThunderPropagator.Channels.ResourceMonitoring.Configuration;
using ThunderPropagator.Channels.ResourceMonitoring.Feeders;
using ThunderPropagator.Channels.ResourceMonitoring.Messages;

namespace ThunderPropagator.UnitTests.Channels.ResourceMonitoring
{
    public sealed class ResourceMonitoringChannelFeederCancellationTests
    {
        [Fact]
        public async Task ReceiveAsync_CancelledDuringDelay_StopsPromptly()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<ResourceMonitoringChannelFeederMessage, ResourceMonitoringChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new ResourceMonitoringChannelConfiguration());
            serviceProvider.RegisterService(Substitute.For<ISystemResourceMonitor>());

            var channel = new ResourceMonitoringChannel(serviceProvider);

            // UtilizationWindow is in seconds and multiplied by 1000 to build the iteration delay —
            // a large window keeps a comfortable gap between "cancel" and "delay would have elapsed".
            var feederConfiguration = new ResourceMonitoringChannelFeederConfiguration { UtilizationWindow = 30 };
            var feederHandler = new NoOpFeederHandler<ResourceMonitoringChannel, ResourceMonitoringChannelFeederMessage>();

            var feeder = new ResourceMonitoringChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);

            using var cancellationTokenSource = new CancellationTokenSource();

            await FeederCancellationTestHelper.AssertCancelledDuringDelayAsync<ResourceMonitoringChannelFeederMessage>(
                feeder,
                cancellationTokenSource,
                cancelAfter: TimeSpan.FromMilliseconds(50),
                promptTimeout: TimeSpan.FromSeconds(2));
        }
    }
}
