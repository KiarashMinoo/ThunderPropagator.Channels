# Demo

## Contents

- [Overview](#overview)
- [Demo Projects](#demo-projects)
- [Common Patterns](#common-patterns)
- [RapidStreamer Dependencies](#rapidstreamer-dependencies)
- [Getting Started](#getting-started)
- [See Also](#see-also)

## Overview

The Demo collection showcases practical implementations of RapidStreamer channels for common business scenarios. Each demo provides a complete, working example with realistic data generation, demonstrating best practices for channel development and real-world application patterns.

## Demo Projects

### [Airport](./Airport/README.md)
Flight information system demonstrating:
- Real-time flight status updates
- Flight schedule management
- Status enumeration (on-time, delayed, cancelled)
- Airport information display patterns

### [Portfolio](./Portfolio/README.md)
Financial portfolio management showcasing:
- Advanced channel implementation with Bogus data generation
- Background simulation threads
- Snapshot management and duplicate key prevention
- Portfolio tracking and real-time updates
- Exception handling patterns

### [StockListBasic](./StockListBasic/README.md)
Simple stock market data demonstrating:
- Basic stock listing and price updates
- Entry-level financial data streaming
- Simplified channel implementation patterns

## Common Patterns

### Demo Channel Structure
All demo channels follow consistent patterns:

```csharp
public class DemoChannel : AbstractChannel<DemoChannelMetadata, DemoChannelConfiguration>
{
    public DemoChannel(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        // Demo-specific initialization
    }
}
```

### Service Registration
```csharp
services.AddAirportDemoChannel(config => 
{
    config.IsEnabled = true;
    config.FeederConfiguration.IsEnabled = true;
});
```

### Data Generation
Many demos use the Bogus library for realistic test data:
```csharp
var faker = new Faker<DemoMessage>()
    .RuleFor(x => x.Name, f => f.Company.CompanyName())
    .RuleFor(x => x.Price, f => f.Random.Decimal(1M, 100M));
```

### Background Simulation
```csharp
public DemoChannel(IServiceProvider serviceProvider) : base(serviceProvider)
{
    _cancellationToken = serviceProvider
        .GetRequiredService<IHostApplicationLifetime>()
        .ApplicationStopping;
        
    new Thread(SimulateData).Start();
}
```

## RapidStreamer Dependencies

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |
| Bogus | Latest | Fake data generation (Portfolio demo) | [NuGet](https://www.nuget.org/packages/Bogus/) |

## Getting Started

### 1. Choose a Demo
Select a demo that matches your use case:
- **Learning**: Start with StockListBasic for simple patterns
- **Financial Apps**: Use Portfolio for advanced financial scenarios
- **Transportation**: Use Airport for scheduling and status systems

### 2. Install and Register
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add your chosen demo channel
    services.AddPortfolioDemoChannel(config => 
    {
        config.IsEnabled = true;
    });
}
```

### 3. Subscribe and Handle Messages
```csharp
public class DemoService
{
    public async Task StartAsync(PortfolioDemoChannel channel)
    {
        await channel.SubscribeAsync("demo-subscriber", message => 
        {
            // Handle demo messages
            ProcessDemoData(message);
        });
    }
}
```

### 4. Extend for Your Needs
Use demos as starting points for custom implementations:
```csharp
public class MyCustomChannel : AbstractChannel<MyMetadata, MyConfiguration>
{
    // Extend demo patterns for your specific requirements
}
```

## See Also

- [../Channels/README.md](../Channels/README.md) — Production-ready channels
- [../Games/README.md](../Games/README.md) — Game-specific implementations

[↑ Back to top](#contents)