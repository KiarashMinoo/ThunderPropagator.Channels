using System.Net.Http;
using Xunit;
using ThunderPropagator.Channels.TimeZones.WeatherApi;

namespace ThunderPropagator.UnitTests.TimeZones.WeatherApi
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
