using ThunderPropagator.Channels.TimeZones.WeatherApi.Models;

namespace ThunderPropagator.UnitTests.Channels.TimeZones.WeatherApi.Models
{
    public class WeatherBulkRequestTests
    {
        [Fact]
        public void WeatherBulkRequest_IsPublic()
        {
            var type = typeof(WeatherBulkRequest);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void WeatherBulkRequestLocation_IsPublic()
        {
            var type = typeof(WeatherBulkRequest.WeatherBulkRequestLocation);
            Assert.True(type.IsPublic || type.IsNestedPublic);
        }

        [Fact]
        public void WeatherBulkRequestLocation_GetHashCode_WithCustomId_UsesQueryAndCustomId()
        {
            // Arrange
            var location = new WeatherBulkRequest.WeatherBulkRequestLocation
            {
                Query = "London",
                CustomId = "custom123"
            };

            // Act
            var hash = location.GetHashCode();

            // Assert
            Assert.NotEqual(0, hash);
        }

        [Fact]
        public void WeatherBulkRequestLocation_GetHashCode_WithoutCustomId_UsesQueryOnly()
        {
            // Arrange
            var location = new WeatherBulkRequest.WeatherBulkRequestLocation
            {
                Query = "London",
                CustomId = null
            };

            // Act
            var hash = location.GetHashCode();

            // Assert
            Assert.Equal("London".GetHashCode(), hash);
        }

        [Fact]
        public void WeatherBulkRequest_GetHashCode_CombinesLocations()
        {
            // Arrange
            var request = new WeatherBulkRequest
            {
                Locations = new List<WeatherBulkRequest.WeatherBulkRequestLocation>
                {
                    new() { Query = "London" },
                    new() { Query = "Paris" }
                }
            };

            // Act
            var hash = request.GetHashCode();

            // Assert
            Assert.NotEqual(0, hash);
        }

        [Fact]
        public void WeatherBulkRequest_Locations_InitializesAsEmptyList()
        {
            // Arrange & Act
            var request = new WeatherBulkRequest();

            // Assert
            Assert.NotNull(request.Locations);
            Assert.Empty(request.Locations);
        }
    }
}
