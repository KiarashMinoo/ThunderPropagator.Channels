using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Throughput;
using ThunderPropagator.UnitTests.Feeders;

namespace ThunderPropagator.UnitTests.Channels.Throughput
{
    public sealed class ThroughputChannelFeederCancellationTests
    {
        [Fact]
        public async Task ReceiveAsync_CancelledDuringDelay_StopsPromptly()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<ThroughputChannelFeederMessage, ThroughputChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new ThroughputChannelConfiguration());

            var channel = new ThroughputChannel(serviceProvider);
            var feederConfiguration = new ThroughputChannelFeederConfiguration();
            var feederHandler = new NoOpFeederHandler<ThroughputChannel, ThroughputChannelFeederMessage>();

            var feeder = new ThroughputChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);

            using var cancellationTokenSource = new CancellationTokenSource();

            // ThroughputChannelFeeder delays 1s before yielding; cancel well before that elapses and
            // confirm the enumeration is cancelled promptly instead of waiting out the delay.
            await FeederCancellationTestHelper.AssertCancelledDuringDelayAsync<ThroughputChannelFeederMessage>(
                feeder,
                cancellationTokenSource,
                cancelAfter: TimeSpan.FromMilliseconds(50),
                promptTimeout: TimeSpan.FromSeconds(2));
        }
    }
}
