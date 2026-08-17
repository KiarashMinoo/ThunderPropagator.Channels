using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.NetworkMonitoring;
using ThunderPropagator.UnitTests.Feeders;

namespace ThunderPropagator.UnitTests.Channels.NetworkMonitoring
{
    public sealed class NetworkMonitoringChannelFeederSubscriptionGatingTests
    {
        private static (NetworkMonitoringChannelFeeder Feeder, NetworkMonitoringChannel Channel) CreateFeeder()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<NetworkMonitoringChannelFeederMessage, NetworkMonitoringChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new NetworkMonitoringChannelConfiguration());

            var channel = new NetworkMonitoringChannel(serviceProvider);
            var feederConfiguration = new NetworkMonitoringChannelFeederConfiguration();
            var feederHandler = new NoOpFeederHandler<NetworkMonitoringChannel, NetworkMonitoringChannelFeederMessage>();

            var feeder = new NetworkMonitoringChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);
            return (feeder, channel);
        }

        private static async Task<int> CountEmittedAsync(NetworkMonitoringChannelFeeder feeder)
        {
            var count = 0;
            await foreach (var _ in FeederCancellationTestHelper.InvokeReceiveAsync<NetworkMonitoringChannelFeederMessage>(feeder, CancellationToken.None))
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
