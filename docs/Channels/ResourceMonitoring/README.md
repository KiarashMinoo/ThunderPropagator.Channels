# ResourceMonitoring Channel

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

The ResourceMonitoring Channel provides comprehensive system resource monitoring including CPU usage, memory consumption, storage utilization, and process/thread counts. It features configurable thresholds for alerting and tracks both percentage-based and absolute resource metrics.

## Files

| File | Primary type(s) | LOC (approx) | Responsibility |
|------|----------------|--------------|----------------|
| ResourceMonitoringChannel.cs | ResourceMonitoringChannel | 15 | Core channel implementation for resource monitoring |
| ResourceMonitoringChannelConfiguration.cs | ResourceMonitoringChannelConfiguration | 15 | Channel configuration with feeder settings |
| ResourceMonitoringChannelExtensions.cs | ResourceMonitoringChannelExtensions | 25 | Service collection extensions for DI registration |
| ResourceMonitoringChannelFeeder.cs | ResourceMonitoringChannelFeeder | 70 | System resource collection feeder |
| ResourceMonitoringChannelFeederConfiguration.cs | ResourceMonitoringChannelFeederConfiguration | 20 | Configuration with threshold settings |
| ResourceMonitoringChannelFeederMessage.cs | ResourceMonitoringChannelFeederMessage | 85 | Message payload containing resource metrics |
| ResourceMonitoringChannelMetadata.cs | ResourceMonitoringChannelMetadata | 70 | Channel metadata and program descriptors |

## Types & Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|------|------|---------|-------------------|-------------|
| ResourceMonitoringChannel | Class | Core resource monitoring channel | AbstractChannel | Constructor |
| ResourceMonitoringChannelFeederConfiguration | Class | Configuration with utilization thresholds | AbstractFeederConfiguration | UtilizationWindow, MemoryUsedPercentageThreshold, StorageUsedPercentageThreshold |
| ResourceMonitoringChannelFeederMessage | Class | Comprehensive resource metrics payload | FeederMessage | CpuUsedPercentage, MemoryUsedPercentage, Alert, Processes, Threads |

### ResourceMonitoringChannelFeederMessage

**Key Properties**:
- `Key : string` — Resource monitoring identifier
- `Alert : string?` — Alert message when thresholds exceeded
- `DateTime : string` — Timestamp of measurement
- `CpuUsedPercentage : double` — CPU utilization percentage
- `MemoryUsedPercentage : double` — Memory utilization percentage
- `MemoryUsedInBytes : double` — Absolute memory usage
- `GuaranteedMemoryInBytes : ulong` — Minimum memory allocation
- `MaximumMemoryInBytes : double` — Maximum memory limit
- `GuaranteedCpuUnits : double` — Minimum CPU allocation
- `MaximumCpuUnits : double` — Maximum CPU limit
- `Processes : decimal` — Active process count
- `Threads : decimal` — Active thread count

### ResourceMonitoringChannelFeederConfiguration

**Key Properties**:
- `UtilizationWindow : int` — Monitoring window in seconds (default: 1)
- `MemoryUsedPercentageThreshold : sbyte` — Memory alert threshold (default: 80%)
- `StorageUsedPercentageThreshold : sbyte` — Storage alert threshold (default: 80%)

## Configuration

```csharp
services.AddResourceMonitoringChannel(config => 
{
    config.FeederConfiguration.MemoryUsedPercentageThreshold = 85;
    config.FeederConfiguration.StorageUsedPercentageThreshold = 90;
    config.FeederConfiguration.UtilizationWindow = 5;
});
```

## Performance Notes

- **Update Frequency**: Configurable through UtilizationWindow
- **Resource Impact**: Moderate CPU/memory overhead during system polling
- **Alerting**: Threshold-based alerts for proactive monitoring
- **Platform**: Cross-platform system metrics collection

## RapidStreamer Dependencies

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |

## Examples

### Resource Monitoring with Alerts

```csharp
await channel.SubscribeAsync("resource-monitor", message => 
{
    if (!string.IsNullOrEmpty(message.Alert))
    {
        logger.LogWarning("Resource Alert: {Alert}", message.Alert);
    }
    
    Console.WriteLine($"CPU: {message.CpuUsedPercentage:F1}%, Memory: {message.MemoryUsedPercentage:F1}%");
    Console.WriteLine($"Processes: {message.Processes}, Threads: {message.Threads}");
});
```

## See Also

- [../NetworkMonitoring/README.md](../NetworkMonitoring/README.md) — Network statistics monitoring
- [../Throughput/README.md](../Throughput/README.md) — Application throughput metrics

[↑ Back to top](#contents)