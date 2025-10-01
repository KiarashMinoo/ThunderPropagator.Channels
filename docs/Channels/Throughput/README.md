# Throughput Channel

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

The Throughput Channel monitors application performance metrics including upstream/downstream message handling, processing sizes, and duration measurements. It provides insights into system throughput and processing efficiency for performance optimization.

## Files

| File | Primary type(s) | LOC (approx) | Responsibility |
|------|----------------|--------------|----------------|
| ThroughputChannel.cs | ThroughputChannel | 15 | Core channel implementation for throughput monitoring |
| ThroughputChannelConfiguration.cs | ThroughputChannelConfiguration | 15 | Channel configuration with feeder settings |
| ThroughputChannelExtensions.cs | ThroughputChannelExtensions | 25 | Service collection extensions for DI registration |
| ThroughputChannelFeeder.cs | ThroughputChannelFeeder | 50 | Throughput metrics collection feeder |
| ThroughputChannelFeederConfiguration.cs | ThroughputChannelFeederConfiguration | 15 | Configuration for throughput feeder |
| ThroughputChannelFeederMessage.cs | ThroughputChannelFeederMessage | 45 | Message payload containing throughput metrics |
| ThroughputChannelMetadata.cs | ThroughputChannelMetadata | 25 | Channel metadata and program descriptors |

## Types & Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|------|------|---------|-------------------|-------------|
| ThroughputChannel | Class | Core throughput monitoring channel | AbstractChannel | Constructor |
| ThroughputChannelFeederMessage | Class | Throughput metrics payload | FeederMessage | UpStreamHandled, DownStreamHandled, DownStreamSize, DownStreamDuration |

### ThroughputChannelFeederMessage

**Key Properties**:
- `Key : string` — Throughput monitoring identifier
- `UpStreamHandled : long` — Number of upstream messages processed
- `DownStreamHandled : long` — Number of downstream messages processed
- `DownStreamSize : double` — Total size of downstream processing
- `DownStreamDuration : long` — Processing duration in milliseconds

## Configuration

```csharp
services.AddThroughputChannel(config => 
{
    config.IsEnabled = true;
    config.FeederConfiguration.IsEnabled = true;
});
```

## Performance Notes

- **Metrics Collection**: Lightweight message counting and timing
- **Overhead**: Minimal impact on application performance
- **Granularity**: Per-operation throughput tracking

## RapidStreamer Dependencies

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |

## Examples

### Throughput Monitoring

```csharp
await channel.SubscribeAsync("throughput-monitor", message => 
{
    var throughputRate = message.DownStreamHandled / (message.DownStreamDuration / 1000.0);
    Console.WriteLine($"Throughput: {throughputRate:F2} messages/sec");
    Console.WriteLine($"Upstream: {message.UpStreamHandled}, Downstream: {message.DownStreamHandled}");
});
```

## See Also

- [../ResourceMonitoring/README.md](../ResourceMonitoring/README.md) — System resource monitoring
- [../NetworkMonitoring/README.md](../NetworkMonitoring/README.md) — Network throughput

[↑ Back to top](#contents)