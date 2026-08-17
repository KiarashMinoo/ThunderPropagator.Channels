using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Clock;
using ThunderPropagator.UnitTests.Feeders;

namespace ThunderPropagator.UnitTests.Channels.Clock
{
    public sealed class UtcNowClockFeederSubscriptionGatingTests
    {
        private static (UtcNowClockFeeder Feeder, ClockChannel Channel) CreateFeeder()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<ClockChannelFeederMessage, UtcNowClockFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new ClockChannelConfiguration());

            var channel = new ClockChannel(serviceProvider);
            var feederConfiguration = new UtcNowClockFeederConfiguration();
            var feederHandler = new NoOpFeederHandler<ClockChannel, ClockChannelFeederMessage>();

            var feeder = new UtcNowClockFeeder(channel, feederConfiguration, feederHandler, serviceProvider);
            return (feeder, channel);
        }

        private static async Task<int> CountEmittedAsync(UtcNowClockFeeder feeder)
        {
            var count = 0;
            await foreach (var _ in FeederCancellationTestHelper.InvokeReceiveAsync<ClockChannelFeederMessage>(feeder, CancellationToken.None))
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
