# Channels

## Contents

- [Overview](#overview)
- [Channel Types](#channel-types)
- [Architecture](#architecture)
- [Common Patterns](#common-patterns)
- [RapidStreamer Dependencies](#rapidstreamer-dependencies)
- [Getting Started](#getting-started)
- [See Also](#see-also)

## Overview

The Channels collection provides pre-built, production-ready streaming channels for common application scenarios. Each channel implements the RapidStreamer framework patterns for real-time data streaming, message broadcasting, and subscription management with built-in configuration, health monitoring, and extensibility.

## Channel Types

### Communication Channels
- **[Chat](./Chat/README.md)** — Real-time messaging with user authentication and group management
- **[Notifications](./Notifications/README.md)** — Sophisticated notification delivery with priority levels and persistence

### Monitoring Channels  
- **[NetworkMonitoring](./NetworkMonitoring/README.md)** — Real-time network usage statistics and bandwidth monitoring
- **[ResourceMonitoring](./ResourceMonitoring/README.md)** — System resource monitoring with CPU, memory, and process metrics
- **[Throughput](./Throughput/README.md)** — Application performance metrics and throughput analysis

### Time-based Channels
- **[Clock](./Clock/README.md)** — High-frequency time streaming with local and UTC time sources
- **[TimeZones](./TimeZones/README.md)** — Multi-timezone time information with integrated weather data

## Architecture

All channels follow consistent architectural patterns:

### Core Components
- **Channel Class** — Inherits from `AbstractChannel<TMetadata, TConfiguration>`
- **Configuration Class** — Extends `AbstractChannelConfiguration` with channel-specific settings
- **Feeder Message** — Inherits from `FeederMessage` defining the data contract
- **Metadata Class** — Extends `AbstractChannelMetadata` with program descriptors
- **Extensions Class** — Provides `IServiceCollection` extension methods

### Feeder Components (where applicable)
- **Feeder Class** — Implements data generation/collection logic
- **Feeder Configuration** — Extends `AbstractFeederConfiguration`

### Common Features
- Thread-safe concurrent operations
- Health monitoring and diagnostics
- Snapshot support for persistence
- Configurable enable/disable functionality
- Integrated dependency injection

## Common Patterns

### Service Registration
```csharp
services.AddChatChannel<MyChatContext>(config => 
{
    config.IsEnabled = true;
});

services.AddClockChannel(config =>
{
    config.NowClockFeederConfiguration.IsEnabled = true;
    config.UtcNowClockFeederConfiguration.IsEnabled = true;
});
```

### Message Subscription
```csharp
await channel.SubscribeAsync("subscriber-id", message => 
{
    // Handle incoming messages
    ProcessMessage(message);
});
```

### Custom Feeders
```csharp
public class CustomFeeder : IterativeFeeder<TChannel, TMessage, TConfig>
{
    protected override async IAsyncEnumerable<FeederReceivedMessage<TMessage>> ReceiveAsync(
        CancellationToken cancellationToken = default)
    {
        // Custom data generation logic
        yield return new TMessage { /* data */ };
    }
}
```

### Configuration Patterns
```csharp
public class MyChannelConfiguration : AbstractChannelConfiguration
{
    public string CustomSetting { get; set; } = "default";
    public int RefreshInterval { get; set; } = 1000;
}
```

## RapidStreamer Dependencies

All channels depend on the core RapidStreamer framework:

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework with channels, feeders, and messaging | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |

### Platform-Specific Packages
The framework supports multiple platforms through conditional package references:
- `RapidStreamer.Debug` / `RapidStreamer` (AnyCPU)
- `RapidStreamer.x64` / `RapidStreamer.Debug.x64` (x64)
- `RapidStreamer.x86` / `RapidStreamer.Debug.x86` (x86)
- `RapidStreamer.ARM64` / `RapidStreamer.Debug.ARM64` (ARM64)

## Getting Started

### 1. Choose Your Channels
Select channels based on your application needs:
- **Real-time Communication**: Chat + Notifications
- **System Monitoring**: NetworkMonitoring + ResourceMonitoring + Throughput
- **Time-sensitive Applications**: Clock + TimeZones

### 2. Install Dependencies
```xml
<PackageReference Include="RapidStreamer" Version="1.0.166-beta.2" />
```

### 3. Register Services
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add required channels
    services.AddClockChannel();
    services.AddNotificationsChannel<MyNotificationConfig>();
    
    // Configure as needed
    services.AddNetworkMonitoringChannel(config => 
    {
        config.FeederConfiguration.IsEnabled = true;
    });
}
```

### 4. Subscribe to Messages
```csharp
public class MyService
{
    public async Task StartAsync(ClockChannel clockChannel)
    {
        await clockChannel.SubscribeAsync("my-subscriber", message => 
        {
            logger.LogInformation("Current time: {Time}", message.DateTime);
        });
    }
}
```

### 5. Emit Custom Messages (if applicable)
```csharp
public class ChatService
{
    public async Task SendMessageAsync(ChatChannel channel, string userId, string message)
    {
        var chatMessage = new ChatChannelFeederMessage
        {
            UserId = userId,
            Message = message,
            DateTime = DateTimeOffset.UtcNow
        };
        
        await channel.EmitMessageAsync(chatMessage);
    }
}
```

## See Also

- [../Demo/README.md](../Demo/README.md) — Demo implementations showcasing channel usage
- [../Games/README.md](../Games/README.md) — Game-specific channel implementations

[↑ Back to top](#contents)