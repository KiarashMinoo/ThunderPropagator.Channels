using ThunderPropagator.Channels.TimeZones.WeatherApi.Models;

namespace ThunderPropagator.UnitTests.Channels.TimeZones.WeatherApi.Models
{
    public class WeatherBulkResponseTests
    {
        [Fact]
        public void WeatherBulkResponse_IsPublic()
        {
            var type = typeof(WeatherBulkResponse);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void WeatherBulkResponseObject_IsPublic()
        {
            var type = typeof(WeatherBulkResponse.WeatherBulkResponseObject);
            Assert.True(type.IsPublic || type.IsNestedPublic);
        }

        [Fact]
        public void WeatherBulkQueryResponse_IsPublic()
        {
            var type = typeof(WeatherBulkResponse.WeatherBulkResponseObject.WeatherBulkQueryResponse);
            Assert.True(type.IsPublic || type.IsNestedPublic);
        }

        [Fact]
        public void WeatherBulkQueryResponse_InheritsFromWeatherResponse()
        {
            var type = typeof(WeatherBulkResponse.WeatherBulkResponseObject.WeatherBulkQueryResponse);
            Assert.True(typeof(WeatherResponse).IsAssignableFrom(type));
        }

        [Fact]
        public void WeatherBulkResponse_Bulk_InitializesAsEmptyList()
        {
            // Arrange & Act
            var response = new WeatherBulkResponse();

            // Assert
            Assert.NotNull(response.Bulk);
            Assert.Empty(response.Bulk);
        }

        [Fact]
        public void WeatherBulkQueryResponse_HasQueryAndCustomIdProperties()
        {
            // Arrange
            var queryResponse = new WeatherBulkResponse.WeatherBulkResponseObject.WeatherBulkQueryResponse
            {
                Query = "London",
                CustomId = "custom123"
            };

            // Assert
            Assert.Equal("London", queryResponse.Query);
            Assert.Equal("custom123", queryResponse.CustomId);
        }
    }
}
