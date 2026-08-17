using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Throughput;
using ThunderPropagator.UnitTests.Feeders;

namespace ThunderPropagator.UnitTests.Channels.Throughput
{
    public sealed class ThroughputChannelFeederSubscriptionGatingTests
    {
        private static (ThroughputChannelFeeder Feeder, ThroughputChannel Channel) CreateFeeder()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<ThroughputChannelFeederMessage, ThroughputChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new ThroughputChannelConfiguration());

            var channel = new ThroughputChannel(serviceProvider);
            var feederConfiguration = new ThroughputChannelFeederConfiguration();
            var feederHandler = new NoOpFeederHandler<ThroughputChannel, ThroughputChannelFeederMessage>();

            var feeder = new ThroughputChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);
            return (feeder, channel);
        }

        private static async Task<int> CountEmittedAsync(ThroughputChannelFeeder feeder)
        {
            var count = 0;
            await foreach (var _ in FeederCancellationTestHelper.InvokeReceiveAsync<ThroughputChannelFeederMessage>(feeder, CancellationToken.None))
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
