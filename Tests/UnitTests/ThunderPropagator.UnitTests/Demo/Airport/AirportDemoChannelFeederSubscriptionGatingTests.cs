using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Demo.Airport;
using ThunderPropagator.UnitTests.Feeders;
using ThunderPropagator.Channels.Demo.Airport.Channel;
using ThunderPropagator.Channels.Demo.Airport.Configuration;
using ThunderPropagator.Channels.Demo.Airport.Feeders;
using ThunderPropagator.Channels.Demo.Airport.Messages;

namespace ThunderPropagator.UnitTests.Demo.Airport
{
    /// <summary>
    /// AirportDemoChannelFeeder's poll delay is a fixed 1 minute, so waiting for a real ReceiveAsync
    /// pass end to end isn't practical in a unit test. These tests instead verify the
    /// subscription-tracking mechanism the guard relies on directly.
    /// </summary>
    public sealed class AirportDemoChannelFeederSubscriptionGatingTests
    {
        private static int GetActiveSubscriptions(AirportDemoChannelFeeder feeder)
        {
            var field = typeof(AirportDemoChannelFeeder).GetField("_activeSubscriptions", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingFieldException(typeof(AirportDemoChannelFeeder).FullName, "_activeSubscriptions");
            return (int)field.GetValue(feeder)!;
        }

        private static (AirportDemoChannelFeeder Feeder, AirportDemoChannel Channel) CreateFeeder()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<AirportDemoChannelFeederMessage, AirportDemoChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new AirportDemoChannelConfiguration());

            var channel = new AirportDemoChannel(serviceProvider);
            var feederConfiguration = new AirportDemoChannelFeederConfiguration();
            var feederHandler = new NoOpFeederHandler<AirportDemoChannel, AirportDemoChannelFeederMessage>();

            var feeder = new AirportDemoChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);
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
