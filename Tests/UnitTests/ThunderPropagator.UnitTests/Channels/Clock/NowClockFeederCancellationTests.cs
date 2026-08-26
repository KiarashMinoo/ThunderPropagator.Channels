using NSubstitute;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Clock;
using ThunderPropagator.UnitTests.Feeders;
using ThunderPropagator.Channels.Clock.Channel;
using ThunderPropagator.Channels.Clock.Configuration;
using ThunderPropagator.Channels.Clock.Feeders;
using ThunderPropagator.Channels.Clock.Messages;

namespace ThunderPropagator.UnitTests.Channels.Clock
{
    public sealed class NowClockFeederCancellationTests
    {
        [Fact]
        public async Task ReceiveAsync_CancelledDuringDelay_StopsPromptly()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<ClockChannelFeederMessage, NowClockFeederConfiguration>();
            var channelConfiguration = new ClockChannelConfiguration();
            serviceProvider.RegisterChannelConfiguration(channelConfiguration);

            var channel = new ClockChannel(serviceProvider);
            var feederConfiguration = new NowClockFeederConfiguration();
            var feederHandler = new NoOpFeederHandler<ClockChannel, ClockChannelFeederMessage>();

            var feeder = new NowClockFeeder(channel, feederConfiguration, feederHandler, serviceProvider);

            using var cancellationTokenSource = new CancellationTokenSource();

            // NowClockFeeder delays 300ms before yielding; cancel well before that elapses and
            // confirm the enumeration is cancelled promptly instead of waiting out the delay.
            await FeederCancellationTestHelper.AssertCancelledDuringDelayAsync<ClockChannelFeederMessage>(
                feeder,
                cancellationTokenSource,
                cancelAfter: TimeSpan.FromMilliseconds(50),
                promptTimeout: TimeSpan.FromSeconds(2));
        }
    }
}
