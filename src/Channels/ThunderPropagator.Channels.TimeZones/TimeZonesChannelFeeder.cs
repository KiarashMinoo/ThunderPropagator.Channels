using System.Runtime.CompilerServices;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.TimeZones;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.TimeZones.WeatherApi;

namespace ThunderPropagator.Channels.TimeZones
{
    internal
#if !DEBUG
        sealed
#endif
        class TimeZonesChannelFeeder : IterativeFeeder<TimeZonesChannel, TimeZonesChannelFeederMessage, TimeZonesChannelFeederConfiguration>
    {
        private readonly WeatherApiService _weatherApiService;

        public TimeZonesChannelFeeder(TimeZonesChannel channel,
            TimeZonesChannelFeederConfiguration feederConfiguration,
            IFeederHandler<TimeZonesChannel, TimeZonesChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            _weatherApiService = serviceProvider.GetRequiredService<WeatherApiService>();

            HealthName = nameof(TimeZonesChannelFeeder);
            HealthTags = [.. HealthTags, "StaticFeeder"];
        }

        protected override async IAsyncEnumerable<FeederReceivedMessage<TimeZonesChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var zoneLocations = Guard.Against.Null(TzdbDateTimeZoneSource.Default.ZoneLocations).ToArray();

            foreach (var source in zoneLocations)
            {
                var weather = await _weatherApiService.GetWeatherOne($"{source.Latitude},{source.Longitude}", cancellationToken);

                foreach (var target in zoneLocations)
                {
                    if (source.ZoneId == target.ZoneId)
                        continue;

                    var now = SystemClock.Instance.GetCurrentInstant();
                    var sourceDateTime = now.InZone(Guard.Against.Null(DateTimeZoneProviders.Tzdb.GetZoneOrNull(source.ZoneId))).ToDateTimeUnspecified();
                    var targetDateTime = now.InZone(Guard.Against.Null(DateTimeZoneProviders.Tzdb.GetZoneOrNull(target.ZoneId))).ToDateTimeUnspecified();

                    yield return new TimeZonesChannelFeederMessage
                    {
                        TimeZone = source.ZoneId,
                        Date = sourceDateTime.Date,
                        Time = sourceDateTime.TimeOfDay,

                        WeatherKey = $"{target.ZoneId}/{DateTime.UtcNow.Hour}",
                        Celsius = weather?.Current?.TempC ?? 0,
                        Fahrenheit = weather?.Current?.TempF ?? 0,
                        Condition = weather?.Current?.Condition?.Text ?? string.Empty,
                        ConditionIcon = weather?.Current?.Condition?.Icon ?? string.Empty,

                        Target = target.ZoneId,
                        TargetDate = targetDateTime.Date,
                        TargetTime = targetDateTime.TimeOfDay,
                    };
                }
            }
        }
    }
}