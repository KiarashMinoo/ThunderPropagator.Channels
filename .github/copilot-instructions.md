# ThunderPropagator.Channels Development Guide

## Project Overview
**ThunderPropagator.Channels** is a .NET library providing production-ready real-time streaming channels (7 channels, 3 demos, 2 games). Built on the ThunderPropagator framework for blazingly fast, cloud-native data streaming with WebSocket-based pub/sub patterns.

## Architecture & Core Patterns

### Channel Structure (Mandatory Components)
Every channel implementation follows this exact structure in `src/Channels/ThunderPropagator.Channels.{Name}/`:

1. **{Name}Channel.cs** — Inherits `AbstractChannel<TMetadata, TConfiguration>`
   - Mark `sealed` in Release builds only: `#if !DEBUG sealed #endif`
   - Override lifecycle methods (`OnSubscriptionRemoved`, etc.) when needed
2. **{Name}ChannelConfiguration.cs** — Extends `AbstractChannelConfiguration`
3. **{Name}ChannelFeederMessage.cs** — Inherits `FeederMessage` (data contract)
4. **{Name}ChannelMetadata.cs** — Extends `AbstractChannelMetadata`
5. **{Name}ChannelExtensions.cs** — DI registration via `IServiceCollection` extensions

### Feeder Pattern (Data Sources)
Feeders generate/collect data for channels. Example: [NowClockFeeder.cs](../src/Channels/ThunderPropagator.Channels.Clock/NowClockFeeder.cs)

- Inherit `IterativeFeeder<TChannel, TMessage, TConfig>`
- Implement `ReceiveAsync` returning `IAsyncEnumerable<FeederReceivedMessage<T>>`
- Register via `AddChannelFeeder<TChannel, TFeeder, TMessage, TConfig>()`
- Configuration classes extend `AbstractFeederConfiguration`

### Pipeline Pattern (Request Handlers)
Bidirectional request/response handlers organized in domain folders. Example: [Chat/Pipelines/Users/Login/](../src/Channels/ThunderPropagator.Channels.Chat/Pipelines/Users/Login/)

- Inherit `AbstractReceivePipeline<TChannel>`
- Use attributes: `[ReceivePipelineRequestSchema]`, `[ReceivePipelineResponseSchema]`
- Set `RequestKey` property for routing (format: `"{Domain}/{Action}"`)
- Register via `AddReceivePipeline<TChannel, TPipeline>()`
- Organize by domain: `Pipelines/{Domain}/{Action}/{ChannelName}ReceiverPipeline.cs`

### DI Registration Pattern
```csharp
public static IServiceCollection Add{Name}Channel(this IServiceCollection services, 
    Action<{Name}ChannelConfiguration>? channelConfigurator = null)
{
    var config = new {Name}ChannelConfiguration();
    channelConfigurator?.Invoke(config);
    
    return services
        .AddSingleton(config)
        .AddChannel<{Name}Channel>()
        .AddChannelFeeder<...>()        // if feeders exist
        .AddReceivePipeline<...>();      // if pipelines exist
}
```

## Build System & Versioning

### Multi-Targeting & Platforms
- **Frameworks**: .NET 8, 9, 10 (`TargetFrameworks` in [Directory.Build.props](../Directory.Build.props))
- **Platforms**: AnyCPU, x86, x64, ARM64
- **Central Package Management**: Version-controlled via [Directory.Packages.props](../Directory.Packages.props)
  - Framework-specific versions: `Condition="'$(TargetFramework)' == 'net9.0'"`
  - ThunderPropagator dependency uses dynamic PackageId: `$(ThunderPropagatorPackageId)`

### Package Naming Convention
Packages include configuration and platform suffixes:
- **Debug**: `{ProjectName}.Debug.{Platform}` (e.g., `ThunderPropagator.Channels.Clock.Debug.x64`)
- **Release**: `{ProjectName}.{Platform}` (AnyCPU omits platform suffix)
- Controlled by: `PackageIdConfigurationSuffix` and `PackageIdPlatformSuffix` in Directory.Build.props

### Version Management
Version: `1.0.1-beta.7` ([Directory.Build.props](../Directory.Build.props#L3))
- Update manually in Directory.Build.props
- Version flows to all projects automatically
- ThunderPropagator dependency version: Separate in Directory.Packages.props (`ThunderPropagatorVersion`)

## Development Workflows

### Building
```powershell
dotnet build ThunderPropagator.Channels.sln -c Release -p:Platform=AnyCPU
```

### Testing
- **Framework**: xUnit with NSubstitute (mocking) and Bogus (fake data)
- **Run**: `dotnet test` or via Visual Studio Test Explorer
- **Structure**: Tests mirror src structure in `Tests/UnitTests/` and `Tests/Demo/`

### Package Publishing
Uses GitHub Packages. See [nuget.config](../nuget.config) for feed configuration.

```powershell
# Pack all platforms (see .github/scripts/pack-all-platforms.ps1)
dotnet pack -c Release -p:Platform=x64
dotnet pack -c Release -p:Platform=ARM64
# ... etc for each platform

# Publish to GitHub Packages
dotnet nuget push "bin/Release/*.nupkg" --source github --api-key $env:GITHUB_TOKEN
```

## Code Conventions

### Conditional Compilation
- **DEBUG vs RELEASE**: Classes are non-sealed in DEBUG for testability:
  ```csharp
  public
  #if !DEBUG
      sealed
  #endif
      class MyChannel : AbstractChannel<...>
  ```

### Documentation & Warnings
- XML documentation enabled (`GenerateDocumentationFile`)
- Suppressed warnings: `CS1591` (missing XML comments), `CS0067` (unused events)
- Unsafe blocks allowed globally (`AllowUnsafeBlocks`)

### Nullability
- Nullable reference types enabled: `<Nullable>enable</Nullable>`
- Implicit usings enabled: `<ImplicitUsings>enable</ImplicitUsings>`

### Telemetry & Observability
All pipelines and feeders include:
- **Activity tracing**: `Telemetry.StartActivity()` with tags
- **Metrics**: Counter creation via `Telemetry.CreateCounter<long>()`
- **Health monitoring**: Set `HealthName` and `HealthTags` in feeders

## Project Organization

```
src/
├── Channels/          # 7 production channels (Chat, Clock, Notifications, etc.)
├── Demo/              # 3 business demos (Airport, Portfolio, StockListBasic)
└── Games/             # 2 interactive games (RockPaperScissors, TicTacToe)

Tests/
├── UnitTests/         # Unit tests organized by category
├── Demo/              # Demo-specific tests
└── ArchTests/         # Architecture validation tests

docs/                  # Comprehensive documentation for each channel/demo/game
```

## External Dependencies
- **Core**: ThunderPropagator framework (GitHub Packages)
- **Testing**: xUnit, NSubstitute, coverlet
- **Utilities**: Bogus (fake data), NodaTime (timezones), JetBrains.Annotations
- **Infrastructure**: Microsoft.Extensions.* (DI, caching, HTTP), Polly (resilience)

## Key Files
- [Directory.Build.props](../Directory.Build.props) — Global MSBuild properties & versioning
- [Directory.Packages.props](../Directory.Packages.props) — Centralized package versions
- [nuget.config](../nuget.config) — NuGet feed configuration (GitHub Packages)
- [global.json](../global.json) — .NET SDK version pinning
- [docs/Channels/README.md](../docs/Channels/README.md) — Channel architecture documentation
