using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Demo.Airport;
using ThunderPropagator.UnitTests.Feeders;

namespace ThunderPropagator.UnitTests.Demo.Airport
{
    public sealed class AirportDemoChannelFeederCancellationTests
    {
        [Fact]
        public async Task ReceiveAsync_CancelledDuringDelay_StopsPromptly()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<AirportDemoChannelFeederMessage, AirportDemoChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new AirportDemoChannelConfiguration());

            var channel = new AirportDemoChannel(serviceProvider);
            var feederConfiguration = new AirportDemoChannelFeederConfiguration();
            var feederHandler = new NoOpFeederHandler<AirportDemoChannel, AirportDemoChannelFeederMessage>();

            var feeder = new AirportDemoChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);

            using var cancellationTokenSource = new CancellationTokenSource();

            // AirportDemoChannelFeeder delays a full minute before yielding; cancel almost immediately
            // and confirm it doesn't wait the delay out.
            await FeederCancellationTestHelper.AssertCancelledDuringDelayAsync<AirportDemoChannelFeederMessage>(
                feeder,
                cancellationTokenSource,
                cancelAfter: TimeSpan.FromMilliseconds(50),
                promptTimeout: TimeSpan.FromSeconds(2));
        }
    }
}
