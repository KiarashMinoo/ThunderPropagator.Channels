using System.Runtime.CompilerServices;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.TimeZones;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.TimeZones.WeatherApi;
using ThunderPropagator.Channels.TimeZones.Channel;
using ThunderPropagator.Channels.TimeZones.Feeders;
using ThunderPropagator.Channels.TimeZones.Messages;

namespace ThunderPropagator.Channels.TimeZones.Feeders
{
    internal
#if !DEBUG
        sealed
#endif
        class TimeZonesChannelFeeder : IterativeFeeder<TimeZonesChannel, TimeZonesChannelFeederMessage, TimeZonesChannelFeederConfiguration>
    {
        private readonly WeatherApiService _weatherApiService;

        // Tracks active subscriptions locally via the channel's public SubscriptionAdded/Removed
        // events, since neither is exposed to feeder code any other way. Read with Volatile.Read
        // and written with Interlocked so the poll loop always sees the latest count.
        private int _activeSubscriptions;

        public TimeZonesChannelFeeder(TimeZonesChannel channel,
            TimeZonesChannelFeederConfiguration feederConfiguration,
            IFeederHandler<TimeZonesChannel, TimeZonesChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            _weatherApiService = serviceProvider.GetRequiredService<WeatherApiService>();

            HealthName = nameof(TimeZonesChannelFeeder);
            HealthTags = [.. HealthTags, "StaticFeeder"];

            channel.SubscriptionAdded += (_, _) => Interlocked.Increment(ref _activeSubscriptions);
            channel.SubscriptionRemoved += (_, _) => Interlocked.Decrement(ref _activeSubscriptions);
        }

        protected override async IAsyncEnumerable<FeederReceivedMessage<TimeZonesChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // This feeder has no poll delay of its own — each pass is paced only by weather-API
            // round-trip latency. Without an idle delay here, an unsubscribed feeder would re-enter
            // ReceiveAsync in a tight loop (no yielded items, nothing to await) and spin a core at
            // 100%. Delay only while idle so the subscribed/active path keeps its existing cadence.
            if (Volatile.Read(ref _activeSubscriptions) <= 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                yield break;
            }

            var zoneLocations = Guard.Against.Null(TzdbDateTimeZoneSource.Default.ZoneLocations).ToArray();

            // Issued concurrently rather than one source at a time (see #18): with dozens of zone
            // locations, awaiting each weather call serially made total push latency the sum of every
            // individual call. WeatherApiService.MaxConcurrentApiCalls is the actual bound on how many
            // of these run against the upstream API at once, so no separate limit is applied here.
            var weathers = await Task.WhenAll(zoneLocations.Select(source =>
                _weatherApiService.GetWeatherOne($"{source.Latitude},{source.Longitude}", cancellationToken)));

            for (var sourceIndex = 0; sourceIndex < zoneLocations.Length; sourceIndex++)
            {
                var source = zoneLocations[sourceIndex];
                var weather = weathers[sourceIndex];

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