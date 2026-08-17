using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Demo.StockListBasic;
using ThunderPropagator.UnitTests.Feeders;

namespace ThunderPropagator.UnitTests.Demo.StockListBasic
{
    /// <summary>
    /// StockListBasicDemoChannelFeeder's poll delay is a random 500-90,000ms and isn't configurable,
    /// so waiting for a real ReceiveAsync pass end to end isn't practical in a unit test. These tests
    /// instead verify the subscription-tracking mechanism the guard relies on directly.
    /// </summary>
    public sealed class StockListBasicDemoChannelFeederSubscriptionGatingTests
    {
        private static int GetActiveSubscriptions(StockListBasicDemoChannelFeeder feeder)
        {
            var field = typeof(StockListBasicDemoChannelFeeder).GetField("_activeSubscriptions", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingFieldException(typeof(StockListBasicDemoChannelFeeder).FullName, "_activeSubscriptions");
            return (int)field.GetValue(feeder)!;
        }

        private static (StockListBasicDemoChannelFeeder Feeder, StockListBasicDemoChannel Channel) CreateFeeder()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<StockListBasicDemoChannelFeederMessage, StockListBasicDemoChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new StockListBasicDemoChannelConfiguration());

            var channel = new StockListBasicDemoChannel(serviceProvider);
            var feederConfiguration = new StockListBasicDemoChannelFeederConfiguration();
            var feederHandler = new NoOpFeederHandler<StockListBasicDemoChannel, StockListBasicDemoChannelFeederMessage>();

            var feeder = new StockListBasicDemoChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);
            return (feeder, channel);
        }

        [Fact]
        public void Constructor_NoSubscriptionsYet_StartsWithZeroActiveSubscriptions()
        {
            var (feeder, _) = CreateFeeder();

            Assert.Equal(0, GetActiveSubscriptions(feeder));
        }

        [Fact]
        public void SubscriptionAdded_IncrementsActiveSubscriptions()
        {
            var (feeder, channel) = CreateFeeder();

            ChannelSubscriptionTestHelper.RaiseSubscriptionAdded(channel);

            Assert.Equal(1, GetActiveSubscriptions(feeder));
        }

        [Fact]
        public void SubscriptionAddedThenRemoved_ReturnsToZeroActiveSubscriptions()
        {
            var (feeder, channel) = CreateFeeder();

            ChannelSubscriptionTestHelper.RaiseSubscriptionAdded(channel);
            ChannelSubscriptionTestHelper.RaiseSubscriptionRemoved(channel);

            Assert.Equal(0, GetActiveSubscriptions(feeder));
        }

        [Fact]
        public void MultipleSubscribeUnsubscribeCycles_NeverGoesNegativeOrStale()
        {
            var (feeder, channel) = CreateFeeder();

            for (var i = 0; i < 5; i++)
            {
                ChannelSubscriptionTestHelper.RaiseSubscriptionAdded(channel);
                Assert.Equal(1, GetActiveSubscriptions(feeder));

                ChannelSubscriptionTestHelper.RaiseSubscriptionRemoved(channel);
                Assert.Equal(0, GetActiveSubscriptions(feeder));
            }
        }
    }
}
