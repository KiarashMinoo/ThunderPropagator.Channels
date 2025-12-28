using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThunderPropagator.Channels.TimeZones.WeatherApi;
using ThunderPropagator.Infrastructure.Extensions;
using System.Net;
using System.Reflection;
using Polly;
using Polly.Extensions.Http;
using ThunderPropagator.BuildingBlocks.Application.Helpers;

namespace ThunderPropagator.Channels.TimeZones
{
    public static class TimeZonesChannelExtensions
    {
        public static IServiceCollection AddTimeZonesChannel(this IServiceCollection services, Action<TimeZonesChannelConfiguration>? channelConfigurator = null)
        {
            TimeZonesChannelConfiguration timeZonesChannelConfiguration = new();
            channelConfigurator?.Invoke(timeZonesChannelConfiguration);

            services
                .AddSingleton(timeZonesChannelConfiguration)
                .AddChannel<TimeZonesChannel>()
                .AddChannelFeeder<TimeZonesChannel, TimeZonesChannelFeeder, TimeZonesChannelFeederMessage, TimeZonesChannelFeederConfiguration>(configuration =>
                {
                    configuration.Bind(timeZonesChannelConfiguration.FeederConfiguration);

                    services
                        .AddStackExchangeRedisCache(options =>
                        {
                            options.Configuration = ConnectionStringHelper.EnrichConnectionString(configuration.RedisCacheConnectionString);
                            options.InstanceName = typeof(TimeZonesChannelExtensions).GetTypeInfo().Namespace;
                        })
                        .AddScoped<CachedWeatherHandler>()
                        .AddHttpClient(nameof(WeatherApiService), client => client.BaseAddress = new Uri(configuration.WeatherApiUrl))
                        .ConfigurePrimaryHttpMessageHandler(_ =>
                        {
                            var httpClientHandler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip };

                            if (!string.IsNullOrWhiteSpace(configuration.Proxy))
                            {
                                httpClientHandler.Proxy = new WebProxy
                                {
                                    Address = new Uri(configuration.Proxy),
                                    BypassProxyOnLocal = false,
                                    UseDefaultCredentials = false
                                };
                            }

                            return httpClientHandler;
                        }).AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3, retryAttempts => TimeSpan.FromSeconds(Math.Pow(2, retryAttempts) / 2)));
                }).TryAddSingleton<WeatherApiService>();

            return services;
        }
    }
}