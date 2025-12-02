using System.Net.Http;
using Xunit;
using RapidStreamer.Channels.TimeZones.WeatherApi;

namespace RapidStreamer.UnitTests.TimeZones.WeatherApi
{
    public class CachedWeatherHandlerTests
    {
        [Fact]
        public void CachedWeatherHandler_Type_IsAvailable()
        {
            var t = typeof(CachedWeatherHandler);
            Assert.NotNull(t);
        }
    }
}
