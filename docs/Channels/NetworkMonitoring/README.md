# NetworkMonitoring Channel

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

The NetworkMonitoring Channel provides real-time network usage statistics by monitoring bytes sent and received across all network interfaces. It tracks delta changes every second, making it suitable for bandwidth monitoring, network usage dashboards, and performance analysis applications.

## Files

| File | Primary type(s) | LOC (approx) | Responsibility |
|------|----------------|--------------|----------------|
| NetworkMonitoringChannel.cs | NetworkMonitoringChannel | 15 | Core channel implementation for network monitoring |
| NetworkMonitoringChannelConfiguration.cs | NetworkMonitoringChannelConfiguration | 15 | Channel configuration with feeder settings |
| NetworkMonitoringChannelExtensions.cs | NetworkMonitoringChannelExtensions | 25 | Service collection extensions for DI registration |
| NetworkMonitoringChannelFeeder.cs | NetworkMonitoringChannelFeeder | 55 | Network statistics collection feeder |
| NetworkMonitoringChannelFeederConfiguration.cs | NetworkMonitoringChannelFeederConfiguration | 15 | Configuration for network monitoring feeder |
| NetworkMonitoringChannelFeederMessage.cs | NetworkMonitoringChannelFeederMessage | 35 | Message payload containing network statistics |
| NetworkMonitoringChannelMetadata.cs | NetworkMonitoringChannelMetadata | 20 | Channel metadata and program descriptors |

## Types & Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|------|------|---------|-------------------|-------------|
| NetworkMonitoringChannel | Class | Core network monitoring channel | AbstractChannel | Constructor |
| NetworkMonitoringChannelConfiguration | Class | Channel configuration with feeder settings | AbstractChannelConfiguration | FeederConfiguration |
| NetworkMonitoringChannelExtensions | Static Class | Service registration extensions | - | AddNetworkMonitoringChannel |
| NetworkMonitoringChannelFeeder | Class | Network statistics collection feeder | IterativeFeeder | ReceiveAsync |
| NetworkMonitoringChannelFeederMessage | Class | Network statistics payload | FeederMessage | Key, DateTime, BytesReceived, BytesSent |

### NetworkMonitoringChannel

- **Kind**: Sealed class (in Release mode)
- **Namespace**: RapidStreamer.Channels.NetworkMonitoring
- **Inherits**: AbstractChannel<NetworkMonitoringChannelMetadata, NetworkMonitoringChannelConfiguration>

**Key Methods**:
- `NetworkMonitoringChannel(IServiceProvider)` — Constructor accepting service provider

**Usage Recipe**:
```csharp
services.AddNetworkMonitoringChannel(config => {
    config.IsEnabled = true;
    config.FeederConfiguration.IsEnabled = true;
});
```

[↑ Back to top](#contents)

### NetworkMonitoringChannelFeederMessage

- **Kind**: Internal sealed class (in Release mode)
- **Namespace**: RapidStreamer.Channels.NetworkMonitoring
- **Inherits**: FeederMessage

**Key Properties**:
- `Key : string` — Fixed identifier "NetworkMonitoring"
- `DateTime : string` — Unix timestamp as string
- `BytesReceived : long` — Delta bytes received since last measurement
- `BytesSent : long` — Delta bytes sent since last measurement

**Constructors**:
- `NetworkMonitoringChannelFeederMessage()` — Default constructor, sets Key to "NetworkMonitoring"

**Usage Recipe**:
```csharp
var message = new NetworkMonitoringChannelFeederMessage
{
    DateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
    BytesReceived = 1024,
    BytesSent = 512
};
```

[↑ Back to top](#contents)

### NetworkMonitoringChannelFeeder

- **Kind**: Internal sealed class (in Release mode)
- **Namespace**: RapidStreamer.Channels.NetworkMonitoring
- **Inherits**: IterativeFeeder<NetworkMonitoringChannel, NetworkMonitoringChannelFeederMessage, NetworkMonitoringChannelFeederConfiguration>

**Key Properties**:
- `HealthName : string` — "NetworkMonitoringChannelFeeder"
- `HealthTags : string[]` — Includes "StaticFeeder"

**Private Fields**:
- `_lastBytesReceived : long` — Tracks previous measurement for delta calculation
- `_lastBytesSent : long` — Tracks previous measurement for delta calculation

**Key Methods**:
- `ReceiveAsync(CancellationToken)` — Collects network interface statistics every second

**Performance Notes**: 
- 1-second polling interval
- Uses NetworkInterface.GetAllNetworkInterfaces() for system-wide statistics
- Calculates deltas to track bandwidth usage changes

**Usage Recipe**:
```csharp
// Automatically registered when AddNetworkMonitoringChannel is called
// Streams network deltas every second
```

[↑ Back to top](#contents)

## Configuration

The network monitoring channel supports basic feeder configuration:

```csharp
public class NetworkMonitoringChannelConfiguration : AbstractChannelConfiguration
{
    public NetworkMonitoringChannelFeederConfiguration FeederConfiguration { get; set; } = new();
}
```

The feeder configuration inherits from `AbstractFeederConfiguration` and can be enabled/disabled.

## Performance Notes

- **Update Frequency**: Statistics collected every 1 second
- **Resource Usage**: Moderate CPU usage when polling network interfaces
- **Network Impact**: Read-only operations, no network traffic generated
- **Memory**: Minimal memory footprint with delta tracking
- **Platform**: Uses .NET NetworkInterface API (cross-platform)

## RapidStreamer Dependencies

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |

## Examples

### Basic Network Monitoring Setup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddNetworkMonitoringChannel(config => 
    {
        config.IsEnabled = true;
        config.FeederConfiguration.IsEnabled = true;
    });
}
```

### Consuming Network Statistics

```csharp
// Subscribe to receive network usage updates
await channel.SubscribeAsync("network-monitor", message => 
{
    var bytesReceivedMB = message.BytesReceived / (1024.0 * 1024.0);
    var bytesSentMB = message.BytesSent / (1024.0 * 1024.0);
    
    Console.WriteLine($"Network Delta - Received: {bytesReceivedMB:F2} MB, Sent: {bytesSentMB:F2} MB");
    Console.WriteLine($"Timestamp: {message.DateTime}");
});
```

### Bandwidth Monitoring Dashboard

```csharp
private readonly List<NetworkUsagePoint> _usageHistory = new();

await channel.SubscribeAsync("bandwidth-dashboard", message => 
{
    var timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(message.DateTime));
    var usage = new NetworkUsagePoint
    {
        Timestamp = timestamp,
        BytesReceived = message.BytesReceived,
        BytesSent = message.BytesSent
    };
    
    _usageHistory.Add(usage);
    
    // Keep only last 60 seconds of data
    var cutoff = DateTimeOffset.UtcNow.AddSeconds(-60);
    _usageHistory.RemoveAll(x => x.Timestamp < cutoff);
    
    // Calculate average bandwidth
    var avgBandwidth = _usageHistory.Average(x => x.BytesReceived + x.BytesSent);
    UpdateDashboard(avgBandwidth);
});
```

## See Also

- [../ResourceMonitoring/README.md](../ResourceMonitoring/README.md) — System resource monitoring
- [../Throughput/README.md](../Throughput/README.md) — Application throughput metrics

[↑ Back to top](#contents)