using NSubstitute;
using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.Demo.Airport.UnitTests
{
    public sealed class AirportDemoChannelFeederTest
    {
        [Fact]
        public void xxx()
        {
            var airportDemoChannel = Substitute.For<AirportDemoChannel>();
            var airportDemoChannelFeederConfiguration = Substitute.For<AirportDemoChannelFeederConfiguration>();
            var feederHandler = Substitute.For<IFeederHandler<AirportDemoChannel, AirportDemoChannelFeederMessage>>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            var airportDemoChannelFeeder = new AirportDemoChannelFeeder(airportDemoChannel, airportDemoChannelFeederConfiguration, feederHandler, serviceProvider);
        }
    }
}