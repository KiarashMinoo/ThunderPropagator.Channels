using ThunderPropagator.Channels.TimeZones.WeatherApi.Models;

namespace ThunderPropagator.UnitTests.Channels.TimeZones.WeatherApi.Models
{
    public class WeatherExceptionTests
    {
        [Fact]
        public void WeatherException_IsPublic()
        {
            var type = typeof(WeatherException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void WeatherExceptionError_IsPublic()
        {
            var type = typeof(WeatherException.WeatherExceptionError);
            Assert.True(type.IsPublic || type.IsNestedPublic);
        }

        [Fact]
        public void WeatherExceptionError_IsSealed()
        {
            var type = typeof(WeatherException.WeatherExceptionError);
            Assert.True(type.IsSealed);
        }

        [Fact]
        public void WeatherExceptionError_HasCodeProperty()
        {
            // Arrange
            var error = new WeatherException.WeatherExceptionError
            {
                Code = 1006,
                Message = "No matching location found."
            };

            // Assert
            Assert.Equal(1006, error.Code);
            Assert.Equal("No matching location found.", error.Message);
        }

        [Fact]
        public void WeatherException_HasErrorProperty()
        {
            // Arrange
            var exception = new WeatherException
            {
                Error = new WeatherException.WeatherExceptionError
                {
                    Code = 1006,
                    Message = "No matching location found."
                }
            };

            // Assert
            Assert.NotNull(exception.Error);
            Assert.Equal(1006, exception.Error.Code);
            Assert.Equal("No matching location found.", exception.Error.Message);
        }
    }
}
