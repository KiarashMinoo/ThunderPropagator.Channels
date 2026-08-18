using System.Diagnostics;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Demo.StockListBasic;
using ThunderPropagator.UnitTests.Feeders;

namespace ThunderPropagator.UnitTests.Demo.StockListBasic
{
    /// <summary>
    /// Confirms StockListBasicDemoChannelFeeder reaches a genuine asynchronous wait before doing any
    /// work — not a synchronous busy-spin — by configuring small Min/MaxPollInterval bounds and
    /// asserting a full ReceiveAsync pass takes at least the configured minimum. The default bounds
    /// (500ms-90s) default to 90s in the worst case, which is exactly why they need to be configurable
    /// to test this at all without a slow test.
    /// </summary>
    public sealed class StockListBasicDemoChannelFeederPollIntervalTests
    {
        [Fact]
        public async Task ReceiveAsync_HonorsConfiguredPollIntervalBounds_TakesAtLeastTheMinimum()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<StockListBasicDemoChannelFeederMessage, StockListBasicDemoChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new StockListBasicDemoChannelConfiguration());

            var channel = new StockListBasicDemoChannel(serviceProvider);
            var minPollInterval = TimeSpan.FromMilliseconds(100);
            var feederConfiguration = new StockListBasicDemoChannelFeederConfiguration
            {
                MinPollInterval = minPollInterval,
                MaxPollInterval = TimeSpan.FromMilliseconds(150)
            };
            var feederHandler = new NoOpFeederHandler<StockListBasicDemoChannel, StockListBasicDemoChannelFeederMessage>();

            var feeder = new StockListBasicDemoChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);

            var stopwatch = Stopwatch.StartNew();
            await foreach (var _ in FeederCancellationTestHelper.InvokeReceiveAsync<StockListBasicDemoChannelFeederMessage>(feeder, CancellationToken.None))
            {
                // Draining is enough — a synchronous busy-spin would complete before the stopwatch
                // below ever saw the configured minimum elapse.
            }
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed >= minPollInterval,
                $"Expected ReceiveAsync to take at least {minPollInterval}, but it completed in {stopwatch.Elapsed} — suggests a synchronous busy-spin.");
        }
    }
}
