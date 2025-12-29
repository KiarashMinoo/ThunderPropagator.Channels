using ThunderPropagator.Channels.TimeZones.WeatherApi.Models;

namespace ThunderPropagator.Channels.TimeZones.UnitTests.WeatherApi
{
    public class WeatherResponseTests
    {
        [Fact]
        public void WeatherResponse_Type_IsAvailable()
        {
            // Sanity check: type can be referenced from unit tests.
            var t = typeof(WeatherResponse);
            Assert.NotNull(t);
        }
    }
}
