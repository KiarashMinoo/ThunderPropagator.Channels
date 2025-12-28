using Xunit;
using ThunderPropagator.Channels.TimeZones.WeatherApi.Models;

namespace ThunderPropagator.UnitTests.TimeZones.WeatherApi
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
