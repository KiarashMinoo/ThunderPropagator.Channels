using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.NetworkMonitoring;
using ThunderPropagator.UnitTests.Feeders;

namespace ThunderPropagator.UnitTests.Channels.NetworkMonitoring
{
    public sealed class NetworkMonitoringChannelFeederCancellationTests
    {
        [Fact]
        public async Task ReceiveAsync_CancelledDuringDelay_StopsPromptly()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<NetworkMonitoringChannelFeederMessage, NetworkMonitoringChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new NetworkMonitoringChannelConfiguration());

            var channel = new NetworkMonitoringChannel(serviceProvider);
            var feederConfiguration = new NetworkMonitoringChannelFeederConfiguration();
            var feederHandler = new NoOpFeederHandler<NetworkMonitoringChannel, NetworkMonitoringChannelFeederMessage>();

            var feeder = new NetworkMonitoringChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);

            using var cancellationTokenSource = new CancellationTokenSource();

            // NetworkMonitoringChannelFeeder delays 1s before yielding; cancel well before that elapses
            // and confirm the enumeration is cancelled promptly instead of waiting out the delay.
            await FeederCancellationTestHelper.AssertCancelledDuringDelayAsync<NetworkMonitoringChannelFeederMessage>(
                feeder,
                cancellationTokenSource,
                cancelAfter: TimeSpan.FromMilliseconds(50),
                promptTimeout: TimeSpan.FromSeconds(2));
        }
    }
}
