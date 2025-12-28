# ThunderPropagator.Channels

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4)](https://dotnet.microsoft.com/)
[![Platforms](https://img.shields.io/badge/Platforms-AnyCPU%20%7C%20x86%20%7C%20x64%20%7C%20ARM64-lightgrey)](https://github.com/KiarashMinoo/ThunderPropagator.Channels)

**Redefining real-time data streaming**: effortless, blazingly fast, and cloud-native for maximum impact.

## Overview

**ThunderPropagator.Channels** (Project ARC) is a comprehensive library delivering **12 production-ready real-time streaming implementations** built on the ThunderPropagator framework. This repository showcases blazingly fast, cloud-native WebSocket-based pub/sub patterns across diverse domains—from simple clock feeds to complex multiplayer games.

### What's Included

- **7 Production Channels**: Fully-featured real-time channels (Chat, Clock, NetworkMonitoring, Notifications, ResourceMonitoring, Throughput, TimeZones)
- **3 Business Demos**: Complex domain-driven applications (Airport, Portfolio, StockListBasic)
- **2 Interactive Games**: Multiplayer games with bidirectional communication (RockPaperScissors, TicTacToe)
- **Comprehensive Documentation**: Auto-generated, deep-dive technical docs with Mermaid diagrams
- **Multi-Framework**: Targets .NET 8, 9, and 10
- **Multi-Platform**: Supports AnyCPU, x86, x64, ARM64

## Quick Start

### Prerequisites

- .NET SDK 9.0+ (see [global.json](global.json))
- GitHub Personal Access Token (for ThunderPropagator package access)

### Configure NuGet Source

ThunderPropagator packages are hosted on **GitHub Packages**. Add the source:

```bash
# Set your GitHub token as environment variable
$env:GH_TOKEN = "your_github_token"

# Add GitHub Packages source
dotnet nuget add source "https://nuget.pkg.github.com/KiarashMinoo/index.json" \
    --name github \
    --username YOUR_GITHUB_USERNAME \
    --password $env:GH_TOKEN \
    --store-password-in-clear-text
```

Alternatively, use the included [nuget.config](nuget.config):

```xml
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/KiarashMinoo/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="KiarashMinoo" />
      <add key="ClearTextPassword" value="%GH_TOKEN%" />
    </github>
  </packageSourceCredentials>
</configuration>
```

### Install & Build

```bash
# Clone repository
git clone https://github.com/KiarashMinoo/ThunderPropagator.Channels.git
cd ThunderPropagator.Channels

# Restore packages
dotnet restore

# Build solution (Release mode)
dotnet build -c Release --no-incremental

# Run tests
dotnet test -c Release
```

### Use a Channel

```csharp
using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Clock;

// Register Clock channel
var services = new ServiceCollection();
services.AddClockChannel(config =>
{
    config.IsEnabled = true;
});

var serviceProvider = services.BuildServiceProvider();
var channel = serviceProvider.GetRequiredService<ClockChannel>();

// Subscribe to real-time updates
var subscription = await channel.SubscribeAsync(new Dictionary<string, object>
{
    ["Key"] = "Now"
});

subscription.OnMessage(message =>
{
    var clockMessage = message as ClockChannelFeederMessage;
    Console.WriteLine($"Current time: {clockMessage.DateTime}");
});
```

## Documentation

This repository publishes comprehensive generated documentation under [`/docs`](docs/README.md). The catalog below links to areas and key subfolders with metrics for types, files, and diagrams.

### Documentation Catalog

- **[Channels](docs/Channels/README.md)** `Types:9` `Files:10` `Diagrams:✓`
  - [Chat](docs/Channels/Chat/README.md) `Types:5` `Files:5` `Diagrams:✓`
  - [Clock](docs/Channels/Clock/README.md) `Types:9` `Files:10` `Diagrams:✓`
  - [NetworkMonitoring](docs/Channels/NetworkMonitoring/README.md) `Types:7` `Files:8` `Diagrams:✓`
  - [Notifications](docs/Channels/Notifications/README.md) `Types:7` `Files:8` `Diagrams:✓`
  - [ResourceMonitoring](docs/Channels/ResourceMonitoring/README.md) `Types:7` `Files:8` `Diagrams:✓`
  - [Throughput](docs/Channels/Throughput/README.md) `Types:7` `Files:8` `Diagrams:✓`
  - [TimeZones](docs/Channels/TimeZones/README.md) `Types:8` `Files:11` `Diagrams:✓`

- **[Demo](docs/Demo/README.md)** `Types:0` `Files:0` `Diagrams:✓`
  - [Airport](docs/Demo/Airport/README.md) `Types:4` `Files:0` `Diagrams:✓`
  - [Portfolio](docs/Demo/Portfolio/README.md) `Types:5` `Files:0` `Diagrams:✓`
  - [StockListBasic](docs/Demo/StockListBasic/README.md) `Types:5` `Files:0` `Diagrams:✓`

- **[Games](docs/Games/README.md)** `Types:0` `Files:0` `Diagrams:✓`
  - [RockPaperScissors](docs/Games/RockPaperScissors/README.md) `Types:3` `Files:0` `Diagrams:✓`
  - [TicTacToe](docs/Games/TicTacToe/README.md) `Types:3` `Files:0` `Diagrams:✓`

**Last generated:** December 28, 2025

## Architecture

ThunderPropagator.Channels follows strict architectural patterns enforced through [ArchTests](Tests/ArchTests/ArchitectureTests.cs):

### Channel Structure (Mandatory Components)

Every channel implementation includes:

1. **{Name}Channel.cs** — Inherits `AbstractChannel<TMetadata, TConfiguration>`
2. **{Name}ChannelConfiguration.cs** — Extends `AbstractChannelConfiguration`
3. **{Name}ChannelFeederMessage.cs** — Inherits `FeederMessage` (data contract)
4. **{Name}ChannelMetadata.cs** — Extends `AbstractChannelMetadata`
5. **{Name}ChannelExtensions.cs** — DI registration via `IServiceCollection` extensions

### Patterns

- **Feeder Pattern**: Data sources generating/collecting data (see [NowClockFeeder](src/Channels/ThunderPropagator.Channels.Clock/NowClockFeeder.cs))
- **Pipeline Pattern**: Bidirectional request/response handlers (see [Chat Pipelines](src/Channels/ThunderPropagator.Channels.Chat/Pipelines/))
- **DI Registration**: Fluent configuration via extension methods

## Build System & Versioning

- **Version**: `1.0.1-beta.7` ([Directory.Build.props](Directory.Build.props))
- **Frameworks**: .NET 8, 9, 10 (controlled in [Directory.Build.props](Directory.Build.props))
- **Platforms**: AnyCPU, x86, x64, ARM64
- **Central Package Management**: [Directory.Packages.props](Directory.Packages.props)
- **Package Naming**: Includes configuration and platform suffixes
  - Debug: `{ProjectName}.Debug.{Platform}`
  - Release: `{ProjectName}.{Platform}` (AnyCPU omits platform suffix)

### Building

```powershell
# Build all platforms for Release
dotnet build ThunderPropagator.Channels.sln -c Release -p:Platform=AnyCPU
dotnet build ThunderPropagator.Channels.sln -c Release -p:Platform=x64
dotnet build ThunderPropagator.Channels.sln -c Release -p:Platform=ARM64
```

### Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Package Publishing

```powershell
# Pack all platforms
dotnet pack -c Release -p:Platform=x64
dotnet pack -c Release -p:Platform=ARM64

# Publish to GitHub Packages
dotnet nuget push "bin/Release/*.nupkg" --source github --api-key $env:GITHUB_TOKEN
```

## Project Organization

```
src/
├── Channels/          # 7 production channels
│   ├── ThunderPropagator.Channels.Chat/
│   ├── ThunderPropagator.Channels.Clock/
│   ├── ThunderPropagator.Channels.NetworkMonitoring/
│   ├── ThunderPropagator.Channels.Notifications/
│   ├── ThunderPropagator.Channels.ResourceMonitoring/
│   ├── ThunderPropagator.Channels.Throughput/
│   └── ThunderPropagator.Channels.TimeZones/
├── Demo/              # 3 business demos
│   ├── ThunderPropagator.Channels.Demo.Airport/
│   ├── ThunderPropagator.Channels.Demo.Portfolio/
│   └── ThunderPropagator.Channels.Demo.StockListBasic/
└── Games/             # 2 interactive games
    ├── ThunderPropagator.Channels.Games.RockPaperScissors/
    └── ThunderPropagator.Channels.Games.TicTacToe/

Tests/
├── ArchTests/         # Architecture validation tests
├── UnitTests/         # Comprehensive unit test suites
│   ├── Channels/
│   ├── Demo/
│   └── Games/
└── Demo/              # Demo-specific tests

docs/                  # Auto-generated documentation
├── README.md          # Documentation landing page
├── Channels/          # Channel documentation with Mermaid diagrams
├── Demo/              # Demo documentation
└── Games/             # Game documentation
```

## Dependencies

### Core Framework

- **[ThunderPropagator](https://nuget.pkg.github.com/KiarashMinoo/index.json)** `1.0.1-beta.5+`
  - Core real-time streaming framework
  - WebSocket-based pub/sub infrastructure
  - Channel abstractions and patterns

### Testing & Utilities

- **Testing**: xUnit, NSubstitute (mocking), coverlet (coverage)
- **Utilities**: Bogus (fake data), NodaTime (timezones), JetBrains.Annotations
- **Infrastructure**: Microsoft.Extensions.* (DI, caching, HTTP), Polly (resilience)

See [Directory.Packages.props](Directory.Packages.props) for complete dependency list with framework-specific versions.

## Code Conventions

- **Nullable Reference Types**: Enabled globally
- **Implicit Usings**: Enabled
- **XML Documentation**: Required (generated for NuGet packages)
- **Conditional Compilation**: Classes are non-sealed in DEBUG for testability
- **Telemetry**: All pipelines/feeders include Activity tracing and metrics
- **Health Monitoring**: Feeders expose health endpoints

## Contributing

Contributions are welcome! Please ensure:

1. All architecture tests pass (`dotnet test Tests/ArchTests`)
2. XML documentation is provided for public APIs
3. Follow existing channel/feeder/pipeline patterns
4. Add unit tests for new functionality
5. Update documentation (`docs/` folder)

## License

This project is licensed under the **Apache License 2.0**. See [LICENSE](LICENSE) for details.

## Authors

**ThunderPropagator Corporation** (Project ARC)

Copyright ©2024 ThunderPropagator Corporation

## Links

- **Website**: [https://www.thunderpropagator.com](https://www.thunderpropagator.com)
- **Repository**: [https://github.com/KiarashMinoo/ThunderPropagator.Channels](https://github.com/KiarashMinoo/ThunderPropagator.Channels)
- **NuGet Packages**: [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json)
- **Documentation**: [`/docs`](docs/README.md)
- **Development Guide**: [`.github/copilot-instructions.md`](.github/copilot-instructions.md)

---

**Blazingly fast. Cloud-native. Maximum impact.** 🚀
