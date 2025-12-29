using ThunderPropagator.Channels.TimeZones.WeatherApi.Models;

namespace ThunderPropagator.Channels.TimeZones.UnitTests.WeatherApi
{
    public class WeatherExceptionErrorTests
    {
        [Fact]
        public void WeatherExceptionError_Type_IsAvailable()
        {
            var t = typeof(WeatherException.WeatherExceptionError);
            Assert.NotNull(t);
        }
    }
}
