using System.Net;
using Microsoft.Extensions.Caching.Distributed;

namespace ThunderPropagator.Channels.TimeZones.WeatherApi
{
    public sealed class CachedWeatherHandler : DelegatingHandler
    {
        // A sliding expiration never lapses for a continuously-queried city, serving stale weather
        // data indefinitely (see #16); an absolute expiration guarantees a refresh at least this often.
        private static readonly TimeSpan CacheFreshnessInterval = TimeSpan.FromMinutes(15);

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
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                await _distributedCache.SetStringAsync(request.RequestUri!.ToString(),
                    content,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheFreshnessInterval },
                    cancellationToken);
            }

            return response;
        }
    }
}