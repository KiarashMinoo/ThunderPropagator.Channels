using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Feeders;

namespace ThunderPropagator.Channels.Demo.Airport.UnitTests
{
    public sealed class AirportDemoChannelFeederTest
    {
        [Fact]
        public void Constructor_WithValidDependencies_DoesNotThrow()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(AirportDemoChannelConfiguration)).Returns(new AirportDemoChannelConfiguration());

            FeederMessageDeserializerResolver<AirportDemoChannelFeederMessage, AirportDemoChannelFeederConfiguration> resolver =
                _ => Substitute.For<IFeederMessageDeserializer<AirportDemoChannelFeederMessage, AirportDemoChannelFeederConfiguration>>();
            serviceProvider.GetService(typeof(FeederMessageDeserializerResolver<AirportDemoChannelFeederMessage, AirportDemoChannelFeederConfiguration>)).Returns(resolver);

            var airportDemoChannel = new AirportDemoChannel(serviceProvider);
            var airportDemoChannelFeederConfiguration = new AirportDemoChannelFeederConfiguration();
            var feederHandler = Substitute.For<IFeederHandler<AirportDemoChannel, AirportDemoChannelFeederMessage>>();

            var airportDemoChannelFeeder = new AirportDemoChannelFeeder(airportDemoChannel, airportDemoChannelFeederConfiguration, feederHandler, serviceProvider);

            Assert.NotNull(airportDemoChannelFeeder);
        }
    }
}
