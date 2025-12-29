using ThunderPropagator.Channels.TimeZones.WeatherApi.Models;

namespace ThunderPropagator.UnitTests.Channels.TimeZones.WeatherApi.Models
{
    public class WeatherResponseTests
    {
        [Fact]
        public void WeatherResponse_IsPublic()
        {
            var type = typeof(WeatherResponse);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void WeatherResponseLocation_IsPublic()
        {
            var type = typeof(WeatherResponse.WeatherResponseLocation);
            Assert.True(type.IsPublic || type.IsNestedPublic);
        }

        [Fact]
        public void WeatherResponseCurrent_IsPublic()
        {
            var type = typeof(WeatherResponse.WeatherResponseCurrent);
            Assert.True(type.IsPublic || type.IsNestedPublic);
        }

        [Fact]
        public void WeatherResponseCurrentCondition_IsPublic()
        {
            var type = typeof(WeatherResponse.WeatherResponseCurrent.WeatherResponseCurrentCondition);
            Assert.True(type.IsPublic || type.IsNestedPublic);
        }
    }
}
