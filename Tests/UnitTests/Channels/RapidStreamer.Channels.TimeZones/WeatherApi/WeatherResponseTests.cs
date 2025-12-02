using Xunit;
using RapidStreamer.Channels.TimeZones.WeatherApi.Models;

namespace RapidStreamer.UnitTests.TimeZones.WeatherApi
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
