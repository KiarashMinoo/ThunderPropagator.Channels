using System.Net;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
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

        [Fact]
        public async Task SendAsync_SuccessResponse_IsCached()
        {
            var distributedCache = Substitute.For<IDistributedCache>();
            distributedCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

            var handler = new CachedWeatherHandler(distributedCache)
            {
                InnerHandler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok-body") })
            };
            using var invoker = new HttpMessageInvoker(handler);

            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/weather"), CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await distributedCache.Received(1).SetAsync(
                "https://example.test/weather",
                Arg.Any<byte[]>(),
                Arg.Is<DistributedCacheEntryOptions>(options =>
                    options.AbsoluteExpirationRelativeToNow.HasValue &&
                    options.SlidingExpiration == null),
                Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(HttpStatusCode.TooManyRequests)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        public async Task SendAsync_ErrorResponse_IsNotCached(HttpStatusCode statusCode)
        {
            var distributedCache = Substitute.For<IDistributedCache>();
            distributedCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

            var handler = new CachedWeatherHandler(distributedCache)
            {
                InnerHandler = new StubHttpMessageHandler(new HttpResponseMessage(statusCode) { Content = new StringContent("error-body") })
            };
            using var invoker = new HttpMessageInvoker(handler);

            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/weather"), CancellationToken.None);

            Assert.Equal(statusCode, response.StatusCode);
            await distributedCache.DidNotReceive().SetAsync(
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<DistributedCacheEntryOptions>(),
                Arg.Any<CancellationToken>());
        }

        private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(response);
        }
    }
}
