# TimeZones Channel

## Contents

- [Overview](#overview)
- [Files](#files)
- [Types & Members](#types--members)
- [Configuration](#configuration)
- [Performance Notes](#performance-notes)
- [RapidStreamer Dependencies](#rapidstreamer-dependencies)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The TimeZones Channel provides comprehensive time and weather information across different time zones. It integrates with weather APIs to deliver real-time time zone data combined with weather conditions, supporting Redis caching and snapshot recovery for enhanced performance.

## Files

| File | Primary type(s) | LOC (approx) | Responsibility |
|------|----------------|--------------|----------------|
| TimeZonesChannel.cs | TimeZonesChannel | 15 | Core channel implementation for time zone information |
| TimeZonesChannelConfiguration.cs | TimeZonesChannelConfiguration | 15 | Channel configuration with feeder settings |
| TimeZonesChannelExtensions.cs | TimeZonesChannelExtensions | 30 | Service collection extensions for DI registration |
| TimeZonesChannelFeeder.cs | TimeZonesChannelFeeder | 60 | Time zone and weather data feeder |
| TimeZonesChannelFeederConfiguration.cs | TimeZonesChannelFeederConfiguration | 60 | Configuration with API and caching settings |
| TimeZonesChannelFeederMessage.cs | TimeZonesChannelFeederMessage | 75 | Message payload containing time and weather data |
| TimeZonesChannelMetadata.cs | TimeZonesChannelMetadata | 30 | Channel metadata and program descriptors |
| WeatherApi/ | Various | - | Weather API integration components |

## Types & Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|------|------|---------|-------------------|-------------|
| TimeZonesChannel | Class | Core time zone information channel | AbstractChannel | Constructor |
| TimeZonesChannelFeederConfiguration | Class | Configuration with API and caching settings | AbstractFeederConfiguration | WeatherApiUrl, WeatherApiKey, RedisCacheConnectionString, SnapshotConnectionString |
| TimeZonesChannelFeederMessage | Class | Time and weather information payload | FeederMessage | TimeZone, Date, Time, Celsius, Fahrenheit, Condition, Target |

### TimeZonesChannelFeederMessage

**Key Properties**:
- `TimeZone : string` — Time zone identifier
- `Date : DateTime` — Current date in the time zone
- `Time : TimeSpan` — Current time in the time zone
- `WeatherKey : string` — Weather data identifier
- `Celsius : double` — Temperature in Celsius
- `Fahrenheit : double` — Temperature in Fahrenheit
- `Condition : string` — Weather condition description
- `ConditionIcon : string` — Weather condition icon identifier
- `Target : string` — Target location or identifier
- `TargetDate : DateTime` — Target date
- `TargetTime : TimeSpan` — Target time

### TimeZonesChannelFeederConfiguration

**Key Properties**:
- `Proxy : string?` — HTTP proxy configuration
- `WeatherApiUrl : string` — Weather service API URL
- `WeatherApiKey : string` — API key for weather service
- `RedisCacheConnectionString : string` — Redis cache connection string
- `SnapshotConnectionString : string` — Snapshot storage connection string
- `SnapshotRecoveryStorage : RecoveryStorage` — Recovery storage type
- `SnapshotTtlHours : int` — Snapshot TTL in hours (default: 24)

## Configuration

```csharp
services.AddTimeZonesChannel(config => 
{
    config.FeederConfiguration.WeatherApiKey = "your-api-key";
    config.FeederConfiguration.WeatherApiUrl = "https://api.weather.com";
    config.FeederConfiguration.RedisCacheConnectionString = "localhost:6379";
    config.FeederConfiguration.SnapshotTtlHours = 12;
});
```

## Performance Notes

- **Caching**: Redis integration for weather data caching
- **API Rate Limiting**: Configurable to respect weather API limits
- **Snapshot Recovery**: Persistent storage for reliability
- **TTL Management**: Configurable cache expiration

## RapidStreamer Dependencies

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |

## Examples

### Time Zone with Weather Monitoring

```csharp
await channel.SubscribeAsync("timezone-weather", message => 
{
    Console.WriteLine($"Time Zone: {message.TimeZone}");
    Console.WriteLine($"Local Time: {message.Date:d} {message.Time}");
    Console.WriteLine($"Weather: {message.Condition}, {message.Celsius:F1}°C / {message.Fahrenheit:F1}°F");
});
```

## See Also

- [../Clock/README.md](../Clock/README.md) — Basic time streaming
- [../../Demo/README.md](../../Demo/README.md) — Demo implementations

[↑ Back to top](#contents)