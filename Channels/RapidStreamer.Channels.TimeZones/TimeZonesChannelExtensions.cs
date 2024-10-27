using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RapidStreamer.Channels.TimeZones.WeatherApi;
using RapidStreamer.Infrastructure.Extensions;
using System.Net;
using System.Reflection;
using Polly;
using Polly.Extensions.Http;

namespace RapidStreamer.Channels.TimeZones
{
    public static class TimeZonesChannelExtensions
    {
        public static IServiceCollection AddTimeZonesChannel(this IServiceCollection services, Action<TimeZonesChannelFeederConfiguration> builder)
        {
            services.AddChannel<TimeZonesChannel>()
                .AddChannelFeeder<TimeZonesChannel, TimeZonesChannelFeeder, TimeZonesChannelFeederMessage, TimeZonesChannelFeederConfiguration>(configuration =>
                {
                    builder.Invoke(configuration);

                    services
                        .AddStackExchangeRedisCache(options =>
                        {
                            options.Configuration = configuration.RedisCacheConnectionString;
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