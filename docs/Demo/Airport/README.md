# Demo Airport

## Contents

- [Overview](#overview)
- [Files](#files)
- [Types & Members](#types--members)
- [Configuration](#configuration)
- [RapidStreamer Dependencies](#rapidstreamer-dependencies)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The Airport Demo Channel provides a realistic simulation of airport flight information, including flight statuses, schedules, delays, and cancellations. It demonstrates real-time data streaming for flight tracking applications and airport information displays.

## Files

| File | Primary type(s) | LOC (approx) | Responsibility |
|------|----------------|--------------|----------------|
| AirportDemoChannel.cs | AirportDemoChannel | 15 | Core channel implementation for airport data |
| AirportDemoChannelConfiguration.cs | AirportDemoChannelConfiguration | 15 | Channel configuration settings |
| AirportDemoChannelFeeder.cs | AirportDemoChannelFeeder | 60 | Flight data generation and simulation |
| AirportDemoChannelFeederConfiguration.cs | AirportDemoChannelFeederConfiguration | 15 | Feeder configuration settings |
| AirportDemoChannelFeederMessage.cs | AirportDemoChannelFeederMessage | 40 | Flight information message payload |
| AirportDemoChannelMetadata.cs | AirportDemoChannelMetadata | 20 | Channel metadata and program descriptors |
| AirportDemoExtensions.cs | AirportDemoExtensions | 25 | Service registration extensions |
| Statuses.cs | Statuses | 15 | Flight status enumeration |

## Types & Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|------|------|---------|-------------------|-------------|
| AirportDemoChannel | Class | Core airport simulation channel | AbstractChannel | Constructor |
| Statuses | Enum | Flight status enumeration | - | ScheduledOnTime, ScheduledDelayed, EnRouteOnTime, LandedOnTime, Cancelled |

### Statuses

**Values**:
- `ScheduledOnTime = 0` — Flight scheduled and on time
- `ScheduledDelayed = 1` — Flight scheduled but delayed
- `EnRouteOnTime = 2` — Flight en route and on time
- `EnRouteDelayed = 3` — Flight en route but delayed
- `LandedOnTime = 4` — Flight landed on time
- `LandedDelayed = 5` — Flight landed with delay
- `Cancelled = 6` — Flight cancelled
- `Deleted = 7` — Flight record deleted

## Configuration

```csharp
services.AddAirportDemoChannel(config => 
{
    config.IsEnabled = true;
    config.FeederConfiguration.IsEnabled = true;
});
```

## RapidStreamer Dependencies

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |

## Examples

### Flight Status Monitoring

```csharp
await channel.SubscribeAsync("flight-monitor", message => 
{
    Console.WriteLine($"Flight {message.FlightNumber}: {message.Status}");
    Console.WriteLine($"Departure: {message.DepartureTime}, Arrival: {message.ArrivalTime}");
});
```

## See Also

- [../Portfolio/README.md](../Portfolio/README.md) — Financial portfolio demo
- [../StockListBasic/README.md](../StockListBasic/README.md) — Basic stock listing demo

[↑ Back to top](#contents)