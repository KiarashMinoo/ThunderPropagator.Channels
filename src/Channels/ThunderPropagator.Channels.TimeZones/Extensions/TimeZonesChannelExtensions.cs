using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThunderPropagator.Channels.TimeZones.WeatherApi;
using ThunderPropagator.Infrastructure.Extensions;
using System.Net;
using System.Reflection;
using Polly;
using Polly.Extensions.Http;
using ThunderPropagator.BuildingBlocks.Application.Helpers;
using ThunderPropagator.Channels.TimeZones.Channel;
using ThunderPropagator.Channels.TimeZones.Configuration;
using ThunderPropagator.Channels.TimeZones.Feeders;
using ThunderPropagator.Channels.TimeZones.Messages;

namespace ThunderPropagator.Channels.TimeZones.Extensions
{
    public static class TimeZonesChannelExtensions
    {
        public static IServiceCollection AddTimeZonesChannel(this IServiceCollection services, Action<TimeZonesChannelConfiguration>? channelConfigurator = null)
        {
            TimeZonesChannelConfiguration timeZonesChannelConfiguration = new();
            channelConfigurator?.Invoke(timeZonesChannelConfiguration);

            // #10's own scope: WeatherApiKey no longer ships a hardcoded default, so a feeder a consumer
            // actually enables without supplying one now fails host startup with a clear,
            // property-specific message instead of shipping a shared key baked into source (and every
            // compiled binary/package) or silently making unauthenticated WeatherAPI calls at runtime.
            // Gated on IsEnabled rather than unconditional: a consumer that registers this channel while
            // leaving the feeder disabled (the default) has no runtime path that ever uses this key, so
            // requiring one anyway would be an unrelated breaking change, not a security fix.
            if (timeZonesChannelConfiguration.FeederConfiguration.IsEnabled && string.IsNullOrWhiteSpace(timeZonesChannelConfiguration.FeederConfiguration.WeatherApiKey))
                throw new TimeZonesChannelConfigurationValidationException(nameof(TimeZonesChannelFeederConfiguration.WeatherApiKey), "must be supplied via configuration (environment variable, user secrets, or a secrets manager) when the TimeZones feeder is enabled.");

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