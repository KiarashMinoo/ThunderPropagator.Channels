# Demo Portfolio

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

The Portfolio Demo Channel provides a comprehensive financial portfolio simulation using the Bogus library for realistic data generation. It simulates stock portfolios with real-time price updates, quantity tracking, and portfolio management features including automatic data generation and snapshot management.

## Files

| File | Primary type(s) | LOC (approx) | Responsibility |
|------|----------------|--------------|----------------|
| PortfolioDemoChannel.cs | PortfolioDemoChannel | 115 | Advanced channel with simulation logic and snapshot handling |
| PortfolioDemoChannelConfiguration.cs | PortfolioDemoChannelConfiguration | 15 | Channel configuration settings |
| PortfolioDemoChannelFeederMessage.cs | PortfolioDemoChannelFeederMessage | 40 | Portfolio item message payload |
| PortfolioDemoChannelMetadata.cs | PortfolioDemoChannelMetadata | 20 | Channel metadata and program descriptors |
| PortfolioDemoExtensions.cs | PortfolioDemoExtensions | 25 | Service registration extensions |
| Pipelines/ | Various | - | Portfolio management pipelines |

## Types & Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|------|------|---------|-------------------|-------------|
| PortfolioDemoChannel | Class | Advanced portfolio simulation channel | AbstractChannel | GeneratePrice, OnSubscriptionAdded, Simulate |

### PortfolioDemoChannel

**Constants**:
- `PortfolioDemo : string` — "PortfolioDemo"
- `PortfolioDemoItems : string` — "PortfolioDemoItems"

**Key Methods**:
- `GeneratePrice() : decimal` — Static method generating random prices (1-100)
- `OnSubscriptionAdded(Subscription)` — Handles new subscriptions with duplicate key validation
- `Simulate()` — Background thread for portfolio simulation

**Key Features**:
- **Bogus Integration**: Uses Faker for realistic data generation
- **Duplicate Prevention**: Throws DuplicatedKeyException for existing keys
- **Background Simulation**: Separate thread for continuous updates
- **Snapshot Integration**: Searches and manages portfolio snapshots

## Configuration

```csharp
services.AddPortfolioDemoChannel(config => 
{
    config.IsEnabled = true;
});
```

## Performance Notes

- **Background Processing**: Uses dedicated thread for simulation
- **Bogus Library**: Leverages Faker for realistic test data
- **Snapshot Management**: Efficient duplicate detection and portfolio state management
- **Exception Handling**: Built-in duplicate key protection

## RapidStreamer Dependencies

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |
| Bogus | Latest | Fake data generation | [NuGet](https://www.nuget.org/packages/Bogus/) |

## Examples

### Portfolio Tracking

```csharp
await channel.SubscribeAsync("portfolio-tracker", message => 
{
    var totalValue = message.Price * message.Quantity;
    Console.WriteLine($"{message.Stock}: {message.Quantity} shares @ ${message.Price:F2} = ${totalValue:F2}");
});
```

### Portfolio Subscription with Key

```csharp
try 
{
    await channel.SubscribeAsync("user-portfolio-123", message => 
    {
        // Handle portfolio updates for specific user
        UpdatePortfolioDisplay(message);
    });
}
catch (DuplicatedKeyException ex)
{
    // Handle duplicate subscription attempt
    Console.WriteLine($"Portfolio {ex.Key} already exists");
}
```

## See Also

- [../Airport/README.md](../Airport/README.md) — Airport flight demo
- [../StockListBasic/README.md](../StockListBasic/README.md) — Basic stock listing demo

[↑ Back to top](#contents)