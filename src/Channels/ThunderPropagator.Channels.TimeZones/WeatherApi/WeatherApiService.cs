using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using ThunderPropagator.BuildingBlocks.Application.Helpers;
using ThunderPropagator.Channels.TimeZones.WeatherApi.Models;

namespace ThunderPropagator.Channels.TimeZones.WeatherApi
{
    internal
#if !DEBUG
        sealed
#endif
        class WeatherApiService
    {
        private static readonly SemaphoreSlim Semaphore = new(1, 1);

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
                await Semaphore.WaitAsync(cancellationToken);

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
                Semaphore.Release();
            }
        }

        public async Task<WeatherBulkResponse?> GetWeatherBulk(WeatherBulkRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                await Semaphore.WaitAsync(cancellationToken);

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
                Semaphore.Release();
            }
        }
    }
}