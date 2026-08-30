using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Demo.Airport.Channel;
using ThunderPropagator.Channels.Demo.Airport.Configuration;
using ThunderPropagator.Channels.Demo.Airport.Feeders;
using ThunderPropagator.Channels.Demo.Airport.Messages;

namespace ThunderPropagator.Channels.Demo.Airport.UnitTests.Feeders
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

        private static TimeSpan InvokeSignedWrappedDelta(TimeSpan from, TimeSpan to)
        {
            var method = typeof(AirportDemoChannelFeeder).GetMethod("SignedWrappedDelta", BindingFlags.NonPublic | BindingFlags.Static)!;
            return (TimeSpan)method.Invoke(null, [from, to])!;
        }

        // Issue #25's own bug: comparisons using a plain `to - from` on Departure/TimeOfDay values
        // broke the instant one side crossed midnight relative to the other. These exercise the
        // midnight-safe replacement directly against the exact shape of that failure.
        [Fact]
        public void SignedWrappedDelta_SameDay_ReturnsPlainDifference()
        {
            var delta = InvokeSignedWrappedDelta(TimeSpan.FromHours(10), TimeSpan.FromHours(13));

            Assert.Equal(TimeSpan.FromHours(3), delta);
        }

        [Fact]
        public void SignedWrappedDelta_ToCrossesMidnightForward_ReturnsShortPositiveDelta()
        {
            // "to" (00:10) is numerically far less than "from" (23:50), but is actually only 20
            // minutes later once midnight is crossed - not ~23h40m in the past.
            var delta = InvokeSignedWrappedDelta(TimeSpan.FromHours(23) + TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(10));

            Assert.Equal(TimeSpan.FromMinutes(20), delta);
        }

        [Fact]
        public void SignedWrappedDelta_FromCrossesMidnightForward_ReturnsShortNegativeDelta()
        {
            // The reverse of the case above: "from" (00:10) is numerically far less than "to" (23:50),
            // but "to" is actually only 20 minutes in the past once midnight is crossed.
            var delta = InvokeSignedWrappedDelta(TimeSpan.FromMinutes(10), TimeSpan.FromHours(23) + TimeSpan.FromMinutes(50));

            Assert.Equal(TimeSpan.FromMinutes(-20), delta);
        }

        [Fact]
        public void SignedWrappedDelta_SameValue_ReturnsZero()
        {
            var delta = InvokeSignedWrappedDelta(TimeSpan.FromHours(12), TimeSpan.FromHours(12));

            Assert.Equal(TimeSpan.Zero, delta);
        }
    }
}
