﻿using Xunit;
using ThunderPropagator.Channels.TimeZones.WeatherApi;
using ThunderPropagator.Channels.TimeZones.WeatherApi.Models;

namespace ThunderPropagator.UnitTests.Channels.TimeZones.WeatherApi
{
    public class WeatherApiServiceTests
    {
        [Fact]
        public void WeatherApiService_IsInternal()
        {
            var type = typeof(WeatherApiService);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void CachedWeatherHandler_IsInternal()
        {
            var type = typeof(CachedWeatherHandler);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void WeatherException_IsException()
        {
            var type = typeof(WeatherException);
            Assert.True(typeof(System.Exception).IsAssignableFrom(type));
        }

        [Fact]
        public void WeatherResponse_IsPublic()
        {
            var type = typeof(WeatherResponse);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void WeatherBulkResponse_IsPublic()
        {
            var type = typeof(WeatherBulkResponse);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void WeatherBulkRequest_IsPublic()
        {
            var type = typeof(WeatherBulkRequest);
            Assert.True(type.IsPublic);
        }
    }
}
