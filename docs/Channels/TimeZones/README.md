# TimeZones

## Contents

- [Overview](#overview)
- [Files](#files)
- [Types and Members](#types-and-members)
- [Validation and Constraints](#validation-and-constraints)
- [Performance Notes](#performance-notes)
- [Package Dependencies](#package-dependencies)
- [Diagrams](#diagrams)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The **TimeZones** area groups 1 documented type, including `TimeZonesChannelExtensions`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `AssemblyInfo.cs` | — | 5 | Contains the assembly info implementation or configuration. |
| `ThunderPropagator.Channels.TimeZones.csproj` | — | 29 | Defines project build targets, dependencies, and package metadata. |
| `TimeZonesChannel.cs` | `TimeZonesChannel` | 23 | Defines TimeZonesChannel and its related behavior. |
| `TimeZonesChannelConfiguration.cs` | `TimeZonesChannelConfiguration` | 18 | Defines TimeZonesChannelConfiguration and its related behavior. |
| `TimeZonesChannelExtensions.cs` | `TimeZonesChannelExtensions` | 56 | Defines TimeZonesChannelExtensions and its related behavior. |
| `TimeZonesChannelFeeder.cs` | `TimeZonesChannelFeeder` | 69 | Defines TimeZonesChannelFeeder and its related behavior. |
| `TimeZonesChannelFeederConfiguration.cs` | `TimeZonesChannelFeederConfiguration` | 60 | Defines TimeZonesChannelFeederConfiguration and its related behavior. |
| `TimeZonesChannelFeederMessage.cs` | `TimeZonesChannelFeederMessage` | 80 | Defines TimeZonesChannelFeederMessage and its related behavior. |
| `TimeZonesChannelMetadata.cs` | `TimeZonesChannelMetadata` | 49 | Defines TimeZonesChannelMetadata and its related behavior. |

### Direct child areas

- [WeatherApi](./WeatherApi/README.md) `Types:1` `Files:2`

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`TimeZonesChannelExtensions`](#timezoneschannelextensions) | class | Represents the TimeZonesChannelExtensions class. | — | `AddTimeZonesChannel(…)` |

### TimeZonesChannelExtensions

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.TimeZones`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `AddTimeZonesChannel(…)`
- **Summary:** Represents the TimeZonesChannelExtensions class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve TimeZonesChannelExtensions from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

## Validation and Constraints

Inputs are validated at component boundaries. Callers should provide non-null required values and handle domain or argument exceptions without retrying invalid requests unchanged.

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
  Current["TimeZones"]
  Current --> C0["WeatherApi"]
```

The diagram shows the direct components documented by the **TimeZones** area.

## Examples

Start with `TimeZonesChannelExtensions` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)
- [Chat](../Chat/README.md)
- [Clock](../Clock/README.md)
- [NetworkMonitoring](../NetworkMonitoring/README.md)
- [Notifications](../Notifications/README.md)
- [ResourceMonitoring](../ResourceMonitoring/README.md)
- [Throughput](../Throughput/README.md)

[↑ Back to top](#contents)
