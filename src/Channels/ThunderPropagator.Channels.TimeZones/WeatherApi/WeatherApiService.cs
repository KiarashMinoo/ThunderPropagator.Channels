using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using ThunderPropagator.BuildingBlocks.Application.Helpers;
using ThunderPropagator.Channels.TimeZones.WeatherApi.Models;
using ThunderPropagator.Channels.TimeZones.Feeders;

namespace ThunderPropagator.Channels.TimeZones.WeatherApi
{
    internal
#if !DEBUG
        sealed
#endif
        class WeatherApiService
    {
        // The single concurrency gate for calls to the upstream weather API (see #18): callers such
        // as TimeZonesChannelFeeder issue many calls via Task.WhenAll rather than serially, so this
        // is what actually bounds how many of them hit the upstream API at once. Instance-scoped
        // (see #17) rather than static, since WeatherApiService is registered as a singleton anyway
        // (TimeZonesChannelExtensions.AddTimeZonesChannel) — this only avoids unintended coupling
        // between separately-constructed instances outside DI, e.g. in tests.
        internal const int MaxConcurrentApiCalls = 10;

        private readonly SemaphoreSlim _semaphore = new(MaxConcurrentApiCalls, MaxConcurrentApiCalls);

        private readonly ILogger<WeatherApiService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TimeZonesChannelFeederConfiguration _configuration;

        public WeatherApiService(ILogger<WeatherApiService> logger, IHttpClientFactory httpClientFactory, TimeZonesChannelFeederConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<WeatherResponse?> GetWeatherOne(string query, CancellationToken cancellationToken = default)
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken);

                using var client = _httpClientFactory.CreateClient(nameof(WeatherApiService));
                var weatherResponse = await client.GetAsync($"current.json?key={_configuration.WeatherApiKey}&q={query}", cancellationToken);
                if (weatherResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                return await weatherResponse.Content.ReadFromJsonAsync<WeatherResponse>(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weather API call failed: {Message}", ex.Message);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<WeatherBulkResponse?> GetWeatherBulk(WeatherBulkRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken);

                using var client = _httpClientFactory.CreateClient(nameof(WeatherApiService));
                var weatherRequestContent = new StringContent(request.ToNJson(), Encoding.UTF8, "application/json");
                var weatherResponse = await client.PostAsync($"current.json?key={_configuration.WeatherApiKey}&q=bulk", weatherRequestContent, cancellationToken);
                if (weatherResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                return await weatherResponse.Content.ReadFromJsonAsync<WeatherBulkResponse>(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weather Bulk API call failed: {Message}", ex.Message);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}