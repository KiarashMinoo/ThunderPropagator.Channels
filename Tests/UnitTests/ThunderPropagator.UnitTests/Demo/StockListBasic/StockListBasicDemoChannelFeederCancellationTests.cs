using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Demo.StockListBasic;
using ThunderPropagator.UnitTests.Feeders;

namespace ThunderPropagator.UnitTests.Demo.StockListBasic
{
    public sealed class StockListBasicDemoChannelFeederCancellationTests
    {
        [Fact]
        public async Task ReceiveAsync_CancelledDuringDelay_StopsPromptly()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<StockListBasicDemoChannelFeederMessage, StockListBasicDemoChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new StockListBasicDemoChannelConfiguration());

            var channel = new StockListBasicDemoChannel(serviceProvider);
            var feederConfiguration = new StockListBasicDemoChannelFeederConfiguration();
            var feederHandler = new NoOpFeederHandler<StockListBasicDemoChannel, StockListBasicDemoChannelFeederMessage>();

            var feeder = new StockListBasicDemoChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);

            using var cancellationTokenSource = new CancellationTokenSource();

            // StockListBasicDemoChannelFeeder delays a random 500-90,000ms; cancel almost immediately —
            // well inside even the shortest possible delay — and confirm it doesn't wait the delay out.
            await FeederCancellationTestHelper.AssertCancelledDuringDelayAsync<StockListBasicDemoChannelFeederMessage>(
                feeder,
                cancellationTokenSource,
                cancelAfter: TimeSpan.FromMilliseconds(50),
                promptTimeout: TimeSpan.FromSeconds(2));
        }
    }
}
