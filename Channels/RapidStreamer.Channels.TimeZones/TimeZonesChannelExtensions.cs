using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RapidStreamer.Channels.TimeZones.WeatherApi;
using RapidStreamer.Infrastructure.Extensions;
using System.Net;
using System.Reflection;
using Polly;
using Polly.Extensions.Http;
using RapidStreamer.BuildingBlocks.Application.Helpers;

namespace RapidStreamer.Channels.TimeZones
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
                    configuration.IsEnabled = timeZonesChannelConfiguration.FeederConfiguration.IsEnabled;
                    configuration.Id = timeZonesChannelConfiguration.FeederConfiguration.Id;
                    configuration.SerializerType = timeZonesChannelConfiguration.FeederConfiguration.SerializerType;
                    configuration.EnrichmentScript = timeZonesChannelConfiguration.FeederConfiguration.EnrichmentScript;
                    configuration.MetadataReferences = timeZonesChannelConfiguration.FeederConfiguration.MetadataReferences;
                    configuration.Proxy = timeZonesChannelConfiguration.FeederConfiguration.Proxy;
                    configuration.WeatherApiUrl = timeZonesChannelConfiguration.FeederConfiguration.WeatherApiUrl;
                    configuration.WeatherApiKey = timeZonesChannelConfiguration.FeederConfiguration.WeatherApiKey;
                    configuration.RedisCacheConnectionString = timeZonesChannelConfiguration.FeederConfiguration.RedisCacheConnectionString;
                    configuration.SnapshotConnectionString = timeZonesChannelConfiguration.FeederConfiguration.SnapshotConnectionString;
                    configuration.SnapshotRecoveryStorage = timeZonesChannelConfiguration.FeederConfiguration.SnapshotRecoveryStorage;
                    configuration.SnapshotTtlHours = timeZonesChannelConfiguration.FeederConfiguration.SnapshotTtlHours;

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