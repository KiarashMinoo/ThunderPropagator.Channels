using System.Net;
using Microsoft.Extensions.Caching.Distributed;

namespace RapidStreamer.Channels.TimeZones.WeatherApi
{
    public sealed class CachedWeatherHandler : DelegatingHandler
    {
        private readonly IDistributedCache _distributedCache;

        public CachedWeatherHandler(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cahced = await _distributedCache.GetStringAsync(request.RequestUri!.ToString(), cancellationToken);
            if (!string.IsNullOrWhiteSpace(cahced))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(cahced)
                };
            }

            var response = await base.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            await _distributedCache.SetStringAsync(request.RequestUri!.ToString(),
                content,
                new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(60 - DateTime.UtcNow.Minute) },
                cancellationToken);
            return response;
        }
    }
}