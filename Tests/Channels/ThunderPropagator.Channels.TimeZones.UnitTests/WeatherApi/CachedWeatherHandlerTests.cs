using ThunderPropagator.Channels.TimeZones.WeatherApi;

namespace ThunderPropagator.Channels.TimeZones.UnitTests.WeatherApi
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
