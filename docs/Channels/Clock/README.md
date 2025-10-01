# Clock Channel

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

The Clock Channel provides real-time timestamp streaming with two built-in feeders: local time (Now) and UTC time (UtcNow). It delivers periodic time updates at 300ms intervals, making it ideal for clock displays, scheduling systems, and time-sensitive applications.

## Files

| File | Primary type(s) | LOC (approx) | Responsibility |
|------|----------------|--------------|----------------|
| ClockChannel.cs | ClockChannel | 15 | Core channel implementation for time streaming |
| ClockChannelConfiguration.cs | ClockChannelConfiguration | 15 | Channel configuration with feeder settings |
| ClockChannelExtensions.cs | ClockChannelExtensions | 25 | Service collection extensions for DI registration |
| ClockChannelFeederMessage.cs | ClockChannelFeederMessage | 45 | Message payload containing time information |
| ClockChannelMetadata.cs | ClockChannelMetadata | 20 | Channel metadata and program descriptors |
| NowClockFeeder.cs | NowClockFeeder | 30 | Local time feeder implementation |
| NowClockFeederConfiguration.cs | NowClockFeederConfiguration | 15 | Configuration for local time feeder |
| UtcNowClockFeeder.cs | UtcNowClockFeeder | 30 | UTC time feeder implementation |
| UtcNowClockFeederConfiguration.cs | UtcNowClockFeederConfiguration | 15 | Configuration for UTC time feeder |

## Types & Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|------|------|---------|-------------------|-------------|
| ClockChannel | Class | Core time streaming channel | AbstractChannel | Constructor |
| ClockChannelConfiguration | Class | Channel configuration with feeder settings | AbstractChannelConfiguration | NowClockFeederConfiguration, UtcNowClockFeederConfiguration |
| ClockChannelExtensions | Static Class | Service registration extensions | - | AddClockChannel |
| ClockChannelFeederMessage | Class | Time information payload | FeederMessage | Key, Date, Time, DateTime |
| NowClockFeeder | Class | Local time streaming feeder | IterativeFeeder | ReceiveAsync |
| UtcNowClockFeeder | Class | UTC time streaming feeder | IterativeFeeder | ReceiveAsync |

### ClockChannel

- **Kind**: Sealed class (in Release mode)
- **Namespace**: RapidStreamer.Channels.Clock
- **Inherits**: AbstractChannel<ClockChannelMetadata, ClockChannelConfiguration>

**Key Methods**:
- `ClockChannel(IServiceProvider)` — Constructor accepting service provider

**Usage Recipe**:
```csharp
services.AddClockChannel(config => {
    config.IsEnabled = true;
    config.NowClockFeederConfiguration.IsEnabled = true;
    config.UtcNowClockFeederConfiguration.IsEnabled = true;
});
```

[↑ Back to top](#contents)

### ClockChannelFeederMessage

- **Kind**: Internal sealed class (in Release mode)
- **Namespace**: RapidStreamer.Channels.Clock
- **Inherits**: FeederMessage

**Key Properties**:
- `Key : string` — Identifier for the time source ("Now" or "UtcNow")
- `Date : DateTime` — Date component only
- `Time : TimeSpan` — Time component only
- `DateTime : DateTime` — Complete date and time

**Constructors**:
- `ClockChannelFeederMessage()` — Default constructor
- `ClockChannelFeederMessage(string, DateTime)` — Internal constructor with key and datetime

**Usage Recipe**:
```csharp
var message = new ClockChannelFeederMessage("CustomKey", DateTime.Now);
// Automatically splits into Date and Time components
```

[↑ Back to top](#contents)

### NowClockFeeder

- **Kind**: Internal sealed class (in Release mode)
- **Namespace**: RapidStreamer.Channels.Clock
- **Inherits**: IterativeFeeder<ClockChannel, ClockChannelFeederMessage, NowClockFeederConfiguration>

**Key Properties**:
- `HealthName : string` — "NowClockFeeder"
- `HealthTags : string[]` — Includes "StaticFeeder"

**Key Methods**:
- `ReceiveAsync(CancellationToken)` — Yields local time messages every 300ms

**Performance Notes**: 300ms delay between iterations
**Usage Recipe**:
```csharp
// Automatically registered when AddClockChannel is called
// Streams DateTime.Now every 300ms
```

[↑ Back to top](#contents)

### UtcNowClockFeeder

- **Kind**: Internal sealed class (in Release mode)
- **Namespace**: RapidStreamer.Channels.Clock
- **Inherits**: IterativeFeeder<ClockChannel, ClockChannelFeederMessage, UtcNowClockFeederConfiguration>

**Key Properties**:
- `HealthName : string` — "UtcNowClockFeeder"
- `HealthTags : string[]` — Includes "StaticFeeder"

**Key Methods**:
- `ReceiveAsync(CancellationToken)` — Yields UTC time messages every 300ms

**Performance Notes**: 300ms delay between iterations
**Usage Recipe**:
```csharp
// Automatically registered when AddClockChannel is called
// Streams DateTime.UtcNow every 300ms
```

[↑ Back to top](#contents)

## Configuration

The clock channel supports configuration for both feeders:

```csharp
public class ClockChannelConfiguration : AbstractChannelConfiguration
{
    public NowClockFeederConfiguration NowClockFeederConfiguration { get; set; } = new();
    public UtcNowClockFeederConfiguration UtcNowClockFeederConfiguration { get; set; } = new();
}
```

Both feeder configurations inherit from `AbstractFeederConfiguration` and can be enabled/disabled independently.

## Performance Notes

- **Update Frequency**: Both feeders emit updates every 300ms
- **Resource Usage**: Minimal CPU overhead, no I/O operations
- **Thread Safety**: Built on thread-safe IterativeFeeder base class
- **Memory**: Low memory footprint with simple DateTime messages

## RapidStreamer Dependencies

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |

## Examples

### Basic Clock Channel Setup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddClockChannel(config => 
    {
        config.IsEnabled = true;
        // Both feeders enabled by default
        config.NowClockFeederConfiguration.IsEnabled = true;
        config.UtcNowClockFeederConfiguration.IsEnabled = true;
    });
}
```

### UTC Only Configuration

```csharp
services.AddClockChannel(config => 
{
    config.NowClockFeederConfiguration.IsEnabled = false;
    config.UtcNowClockFeederConfiguration.IsEnabled = true;
});
```

### Consuming Clock Messages

```csharp
// Subscribe to receive time updates
// Messages will arrive every 300ms with current time
await channel.SubscribeAsync("clock-subscriber", message => 
{
    Console.WriteLine($"Time from {message.Key}: {message.DateTime}");
    Console.WriteLine($"Date: {message.Date}, Time: {message.Time}");
});
```

## See Also

- [../TimeZones/README.md](../TimeZones/README.md) — Multi-timezone time information
- [../Chat/README.md](../Chat/README.md) — Message timestamps

[↑ Back to top](#contents)