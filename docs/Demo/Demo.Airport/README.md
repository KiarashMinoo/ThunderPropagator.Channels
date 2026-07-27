# Demo.Airport

## Contents

- [Overview](#overview)
- [Files](#files)
- [Types and Members](#types-and-members)
- [Performance Notes](#performance-notes)
- [Package Dependencies](#package-dependencies)
- [Diagrams](#diagrams)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The **Demo.Airport** area groups 2 documented types, including `AirportDemoExtensions`, `Statuses`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `AirportDemoChannel.cs` | `AirportDemoChannel` | 15 | Defines AirportDemoChannel and its related behavior. |
| `AirportDemoChannelConfiguration.cs` | `AirportDemoChannelConfiguration` | 18 | Defines AirportDemoChannelConfiguration and its related behavior. |
| `AirportDemoChannelFeeder.cs` | `AirportDemoChannelFeeder` | 511 | Defines AirportDemoChannelFeeder and its related behavior. |
| `AirportDemoChannelFeederConfiguration.cs` | `AirportDemoChannelFeederConfiguration` | 17 | Defines AirportDemoChannelFeederConfiguration and its related behavior. |
| `AirportDemoChannelFeederMessage.cs` | `AirportDemoChannelFeederMessage` | 53 | Defines AirportDemoChannelFeederMessage and its related behavior. |
| `AirportDemoChannelMetadata.cs` | `AirportDemoChannelMetadata` | 28 | Defines AirportDemoChannelMetadata and its related behavior. |
| `AirportDemoExtensions.cs` | `AirportDemoExtensions` | 21 | Defines AirportDemoExtensions and its related behavior. |
| `AssemblyInfo.cs` | — | 6 | Contains the assembly info implementation or configuration. |
| `Statuses.cs` | `Statuses` | 14 | Defines Statuses and its related behavior. |
| `ThunderPropagator.Channels.Demo.Airport.csproj` | — | 26 | Defines project build targets, dependencies, and package metadata. |

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`AirportDemoExtensions`](#airportdemoextensions) | class | Represents the AirportDemoExtensions class. | — | `AddAirportDemoChannel(…)` |
| [`Statuses`](#statuses) | enum | Represents the Statuses enum. | — | — |

### AirportDemoExtensions

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Demo.Airport`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `AddAirportDemoChannel(…)`
- **Summary:** Represents the AirportDemoExtensions class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve AirportDemoExtensions from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### Statuses

- **Kind:** enum
- **Namespace:** `ThunderPropagator.Channels.Demo.Airport`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** Refer to the API surface in the source package
- **Summary:** Represents the Statuses enum.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve Statuses from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

## Performance Notes

This area contains performance-sensitive constructs such as pooled buffers, spans, asynchronous value types, or concurrent collections. Avoid unnecessary allocations and blocking calls on streaming or message-processing paths.

## Package Dependencies

| Package | Version | Description | Links |
|---|---|---|---|
| `Bogus` | `35.6.5` | External dependency used by the repository. | [Registry](https://www.nuget.org/packages/Bogus) |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | `10.*` | External dependency used by the repository. | [Registry](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis) |
| `Microsoft.Extensions.Diagnostics.Testing` | `10.*` | External dependency used by the repository. | [Registry](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.Testing) |
| `Microsoft.Extensions.Http.Polly` | `10.*` | External dependency used by the repository. | [Registry](https://www.nuget.org/packages/Microsoft.Extensions.Http.Polly) |
| `NodaTime` | `3.3.2` | External dependency used by the repository. | [Registry](https://www.nuget.org/packages/NodaTime) |

## Diagrams

### Component overview

```mermaid
graph TD
  Current["Demo.Airport"]
  Current --> T0["AirportDemoExtensions"]
  Current --> T1["Statuses"]
```

The diagram shows the direct components documented by the **Demo.Airport** area.

## Examples

Start with `AirportDemoExtensions` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)
- [Demo.Portfolio](../Demo.Portfolio/README.md)
- [Demo.StockListBasic](../Demo.StockListBasic/README.md)

[↑ Back to top](#contents)
