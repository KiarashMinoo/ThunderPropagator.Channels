using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.TimeZones;
using ThunderPropagator.Channels.TimeZones.WeatherApi;
using ThunderPropagator.UnitTests.Feeders;
using ThunderPropagator.Channels.TimeZones.Channel;
using ThunderPropagator.Channels.TimeZones.Configuration;
using ThunderPropagator.Channels.TimeZones.Feeders;
using ThunderPropagator.Channels.TimeZones.Messages;

namespace ThunderPropagator.UnitTests.Channels.TimeZones
{
    public sealed class TimeZonesChannelFeederSubscriptionGatingTests
    {
        [Fact]
        public async Task ReceiveAsync_NoActiveSubscriptions_YieldsNoMessagesAndDoesNotCallWeatherApi()
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<TimeZonesChannelFeederMessage, TimeZonesChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new TimeZonesChannelConfiguration());

            var feederConfiguration = new TimeZonesChannelFeederConfiguration();
            serviceProvider.RegisterService(feederConfiguration);

            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            var weatherApiService = new WeatherApiService(NullLogger<WeatherApiService>.Instance, httpClientFactory, feederConfiguration);
            serviceProvider.RegisterService(weatherApiService);

            var channel = new TimeZonesChannel(serviceProvider);
            var feederHandler = new NoOpFeederHandler<TimeZonesChannel, TimeZonesChannelFeederMessage>();

            var feeder = new TimeZonesChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);

            var count = 0;
            await foreach (var _ in FeederCancellationTestHelper.InvokeReceiveAsync<TimeZonesChannelFeederMessage>(feeder, CancellationToken.None))
                count++;

            Assert.Equal(0, count);

            // No subscription ever existed, so the (otherwise expensive, per-zone-location) weather
            // API path must never have been reached — confirmed by the factory never being asked
            // for a client.
            httpClientFactory.DidNotReceiveWithAnyArgs().CreateClient(default!);
        }
    }
}
