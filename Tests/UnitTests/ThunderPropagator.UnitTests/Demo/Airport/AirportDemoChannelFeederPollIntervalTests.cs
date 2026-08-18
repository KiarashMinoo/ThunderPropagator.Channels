using System.Diagnostics;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Demo.Airport;
using ThunderPropagator.UnitTests.Feeders;

namespace ThunderPropagator.UnitTests.Demo.Airport
{
    /// <summary>
    /// Confirms AirportDemoChannelFeeder reaches a genuine asynchronous wait before doing any work —
    /// not a synchronous busy-spin — by configuring a small PollInterval and asserting a full
    /// ReceiveAsync pass takes at least that long. PollInterval defaults to 1 minute in production,
    /// which is exactly why it needs to be configurable to test this at all without a slow test.
    /// </summary>
    public sealed class AirportDemoChannelFeederPollIntervalTests
    {
        [Fact]
        public async Task ReceiveAsync_HonorsConfiguredPollInterval_TakesAtLeastThatLong()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<AirportDemoChannelFeederMessage, AirportDemoChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new AirportDemoChannelConfiguration());

            var channel = new AirportDemoChannel(serviceProvider);
            var pollInterval = TimeSpan.FromMilliseconds(150);
            var feederConfiguration = new AirportDemoChannelFeederConfiguration { PollInterval = pollInterval };
            var feederHandler = new NoOpFeederHandler<AirportDemoChannel, AirportDemoChannelFeederMessage>();

            var feeder = new AirportDemoChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);

            var stopwatch = Stopwatch.StartNew();
            await foreach (var _ in FeederCancellationTestHelper.InvokeReceiveAsync<AirportDemoChannelFeederMessage>(feeder, CancellationToken.None))
            {
                // Draining is enough — a synchronous busy-spin would complete before the stopwatch
                // below ever saw the configured interval elapse.
            }
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed >= pollInterval,
                $"Expected ReceiveAsync to take at least {pollInterval}, but it completed in {stopwatch.Elapsed} — suggests a synchronous busy-spin.");
        }
    }
}
