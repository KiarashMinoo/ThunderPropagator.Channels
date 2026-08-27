using ThunderPropagator.Channels.TimeZones.Feeders;

namespace ThunderPropagator.UnitTests.Channels.TimeZones
{
    /// <summary>Issue #10: <see cref="TimeZonesChannelFeederConfiguration.WeatherApiKey"/> must never default to a real key.</summary>
    public sealed class TimeZonesChannelFeederConfigurationTests
    {
        [Fact]
        public void WeatherApiKey_Default_IsNull()
        {
            var configuration = new TimeZonesChannelFeederConfiguration();

            Assert.Null(configuration.WeatherApiKey);
        }

        [Fact]
        public void WeatherApiKey_WhenSet_ReturnsTheConfiguredValue()
        {
            var configuration = new TimeZonesChannelFeederConfiguration
            {
                WeatherApiKey = "a-real-key-supplied-via-configuration"
            };

            Assert.Equal("a-real-key-supplied-via-configuration", configuration.WeatherApiKey);
        }
    }
}
