# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```powershell
# Restore packages (also downloads shared build props)
dotnet restore

# Build (Release mode, default AnyCPU)
dotnet build -c Release

# Build specific platform
dotnet build -c Release -p:Platform=x64
dotnet build -c Release -p:Platform=ARM64

# Run all tests
dotnet test -c Release

# Run a single test
dotnet test --filter "FullyQualifiedName~YourTestClassName"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run architecture tests only
dotnet test Tests/UnitTests/ArchTests

# Pack NuGet packages
dotnet pack -c Release -p:Platform=x64
```

## NuGet Source Setup

ThunderPropagator packages are on GitHub Packages. Requires `GH_TOKEN` environment variable:

```powershell
$env:GH_TOKEN = "your_github_token"
dotnet nuget add source "https://nuget.pkg.github.com/KiarashMinoo/index.json" `
    --name github --username KiarashMinoo --password $env:GH_TOKEN --store-password-in-clear-text
```

## Architecture

### Project Layout

```
src/
├── Channels/    # 7 production channels (Chat, Clock, NetworkMonitoring, Notifications,
│                #   ResourceMonitoring, Throughput, TimeZones)
├── Demo/        # 3 business demos (Airport, Portfolio, StockListBasic)
└── Games/       # 2 games (RockPaperScissors, TicTacToe)

Tests/
├── UnitTests/ArchTests/           # NetArchTest architecture validation
├── UnitTests/ThunderPropagator.UnitTests/
├── Channels/                      # Per-channel unit test projects
├── Demo/
└── Games/
```

### Mandatory Channel Structure

Every channel must have exactly these 5 files (enforced by ArchTests):

| File | Base Type | Visibility |
|------|-----------|------------|
| `{Name}Channel.cs` | `AbstractChannel<TMetadata, TConfiguration>` | public, sealed\* |
| `{Name}ChannelConfiguration.cs` | `AbstractChannelConfiguration` | public |
| `{Name}ChannelFeederMessage.cs` | `FeederMessage` | internal |
| `{Name}ChannelMetadata.cs` | `AbstractChannelMetadata` | public |
| `{Name}ChannelExtensions.cs` | static class | public static |

\* Channel classes use `#if !DEBUG sealed #endif` — sealed in Release, non-sealed in Debug for testability.

### Two Core Patterns

**Feeder Pattern** (push-only channels): Inherit `IterativeFeeder<TChannel, TMessage, TConfig>`, implement `ReceiveAsync()` returning `IAsyncEnumerable<FeederReceivedMessage<TMessage>>`. Feeders are `internal` by convention.

```csharp
internal class NowClockFeeder : IterativeFeeder<ClockChannel, ClockChannelFeederMessage, NowClockFeederConfiguration>
{
    protected override async IAsyncEnumerable<FeederReceivedMessage<ClockChannelFeederMessage>> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
        yield return new ClockChannelFeederMessage(...);
    }
}
```

**Pipeline Pattern** (bidirectional channels): Receive pipelines live in `Pipelines/` organized by domain. Each pipeline has `{Name}ReceiverPipeline.cs`, `{Name}ReceiverPipelineRequestDto.cs`, and optionally `{Name}ReceiverPipelineResponseDto.cs`. All three must be `public`.

### FeederMessage Properties

`FeederMessage` is a dictionary-backed class from `ThunderPropagator.BuildingBlocks`. Properties use `GetValueOrDefault<T>()` and `SetValue()`:

```csharp
public string Key
{
    get => GetValueOrDefault(string.Empty);
    private set => SetValue(value);
}
```

### DI Registration

Each channel registers via its `Extensions` class using `AddChannel<T>()` and `AddChannelFeeder<...>()` from `ThunderPropagator.Infrastructure.Extensions`:

```csharp
services
    .AddSingleton(channelConfiguration)
    .AddChannel<ClockChannel>()
    .AddChannelFeeder<ClockChannel, NowClockFeeder, ClockChannelFeederMessage, NowClockFeederConfiguration>(...);
```

## Build System

### Shared Props

`Directory.Build.props` auto-downloads `Shared.Build.props` and `Shared.Nuget.props` from `https://github.com/KiarashMinoo/ThunderPropagator.SharedBuild` into `.shared-props/` (3 retry attempts). Run `dotnet clean` to purge them; `dotnet restore` re-downloads. If downloads fail, check network/GH_TOKEN.

### Versioning

Version is set in `Directory.Build.props` under `<Version>`. CI manages bumps automatically — do not edit manually outside of release workflows. The `develop` branch triggers beta CI (increments beta suffix); `release/**` branches trigger release CI (strips beta suffix and publishes).

### Package IDs

Package names are dynamic: `ThunderPropagator$(PackageIdConfigurationSuffix)$(PackageIdPlatformSuffix)`. Debug builds append `.Debug`; AnyCPU omits the platform suffix.

## Architecture Rules (Enforced by ArchTests)

- `Channels` namespace must not depend on `Demo` or `Games` namespaces
- `Demo` and `Games` namespaces must not cross-depend
- Classes ending with `Channel` must be abstract or sealed
- Classes ending with `Configuration`, `Feeder`, `Pipeline`, `Metadata`, `FeederMessage`, `PipelineRequestDto`, `PipelineResponseDto` must be public
- Classes ending with `Extensions` must be static and public
- Classes ending with `Exception` must inherit from `System.Exception`

## Code Conventions

- Nullable reference types and implicit usings are enabled globally
- Internal fields use `_camelCase` prefix
- Telemetry activity names follow `{ClassName}_{MethodName}` convention
- Platform names use `MacOs` not `MacOS`, `onAcPower` not `onACPower`
- XML documentation is required for all public APIs (`GenerateDocumentationFile=true`)
- `EnablePreviewFeatures=true` is set only in test projects