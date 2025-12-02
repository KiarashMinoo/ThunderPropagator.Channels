using Xunit;
using RapidStreamer.Channels.TimeZones.WeatherApi.Models;

namespace RapidStreamer.UnitTests.TimeZones.WeatherApi
{
    public class WeatherExceptionErrorTests
    {
        [Fact]
        public void WeatherExceptionError_Type_IsAvailable()
        {
            var t = typeof(WeatherExceptionError);
            Assert.NotNull(t);
        }
    }
}
