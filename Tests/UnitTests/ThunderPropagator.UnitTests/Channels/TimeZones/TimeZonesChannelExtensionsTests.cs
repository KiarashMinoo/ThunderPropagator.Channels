using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.TimeZones.Configuration;
using ThunderPropagator.Channels.TimeZones.Extensions;
using ThunderPropagator.Channels.TimeZones.Feeders;

namespace ThunderPropagator.UnitTests.Channels.TimeZones
{
    /// <summary>
    /// Issue #10: <see cref="TimeZonesChannelFeederConfiguration.WeatherApiKey"/> no longer ships a
    /// hardcoded default (previously baked into source and every compiled binary/NuGet package built
    /// from this repo) — <see cref="TimeZonesChannelExtensions.AddTimeZonesChannel"/> now fails host
    /// startup with a property-specific error instead when a consumer actually enables the feeder
    /// without supplying one.
    /// </summary>
    public sealed class TimeZonesChannelExtensionsTests
    {
        [Fact]
        public void AddTimeZonesChannel_WithFeederLeftDisabled_AndNoApiKey_DoesNotThrow()
        {
            var services = new ServiceCollection();

            // The feeder's own default: IsEnabled = false — a consumer that registers this channel
            // without turning the feeder on has no runtime path that ever uses the key, so requiring one
            // anyway would be an unrelated breaking change, not this issue's own security fix.
            var exception = Record.Exception(() => services.AddTimeZonesChannel());

            Assert.Null(exception);
        }

        [Fact]
        public void AddTimeZonesChannel_WithFeederEnabled_AndNoApiKey_ThrowsWithThePropertyNamed()
        {
            var services = new ServiceCollection();

            var exception = Assert.Throws<TimeZonesChannelConfigurationValidationException>(() =>
                services.AddTimeZonesChannel(configuration => configuration.FeederConfiguration.IsEnabled = true));

            Assert.Equal(nameof(TimeZonesChannelFeederConfiguration.WeatherApiKey), exception.PropertyName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddTimeZonesChannel_WithFeederEnabled_AndABlankApiKey_Throws(string? apiKey)
        {
            var services = new ServiceCollection();

            Assert.Throws<TimeZonesChannelConfigurationValidationException>(() =>
                services.AddTimeZonesChannel(configuration =>
                {
                    configuration.FeederConfiguration.IsEnabled = true;
                    configuration.FeederConfiguration.WeatherApiKey = apiKey;
                }));
        }

        [Fact]
        public void AddTimeZonesChannel_WithFeederEnabled_AndAnApiKeyProvided_DoesNotThrow()
        {
            var services = new ServiceCollection();

            var exception = Record.Exception(() =>
                services.AddTimeZonesChannel(configuration =>
                {
                    configuration.FeederConfiguration.IsEnabled = true;
                    configuration.FeederConfiguration.WeatherApiKey = "a-real-key-supplied-via-configuration";
                }));

            Assert.Null(exception);
        }
    }
}
