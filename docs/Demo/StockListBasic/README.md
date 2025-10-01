# Demo StockListBasic

## Contents

- [Overview](#overview)
- [Files](#files)
- [Types & Members](#types--members)
- [Configuration](#configuration)
- [RapidStreamer Dependencies](#rapidstreamer-dependencies)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The StockListBasic Demo Channel provides a simplified stock listing demonstration, offering basic stock market data streaming. It serves as an entry-level example for financial data applications and stock market monitoring systems.

## Files

| File | Primary type(s) | LOC (approx) | Responsibility |
|------|----------------|--------------|----------------|
| StockListBasicDemoChannel.cs | StockListBasicDemoChannel | 15 | Core channel implementation for basic stock data |
| StockListBasicDemoChannelConfiguration.cs | StockListBasicDemoChannelConfiguration | 15 | Channel configuration settings |
| StockListBasicDemoChannelFeeder.cs | StockListBasicDemoChannelFeeder | 40 | Basic stock data generation feeder |
| StockListBasicDemoChannelFeederConfiguration.cs | StockListBasicDemoChannelFeederConfiguration | 15 | Feeder configuration settings |
| StockListBasicDemoChannelFeederMessage.cs | StockListBasicDemoChannelFeederMessage | 30 | Stock information message payload |
| StockListBasicDemoChannelMetadata.cs | StockListBasicDemoChannelMetadata | 20 | Channel metadata and program descriptors |
| StockListBasicDemoExtensions.cs | StockListBasicDemoExtensions | 25 | Service registration extensions |

## Types & Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|------|------|---------|-------------------|-------------|
| StockListBasicDemoChannel | Class | Basic stock listing channel | AbstractChannel | Constructor |

## Configuration

```csharp
services.AddStockListBasicDemoChannel(config => 
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

### Basic Stock Monitoring

```csharp
await channel.SubscribeAsync("stock-monitor", message => 
{
    Console.WriteLine($"{message.Symbol}: ${message.Price:F2}");
    Console.WriteLine($"Change: {message.Change:+0.00;-0.00}");
});
```

## See Also

- [../Portfolio/README.md](../Portfolio/README.md) — Advanced portfolio demo
- [../Airport/README.md](../Airport/README.md) — Airport flight demo

[↑ Back to top](#contents)