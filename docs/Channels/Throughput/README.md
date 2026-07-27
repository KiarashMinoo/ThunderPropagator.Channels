# Throughput

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

The **Throughput** area groups 1 documented type, including `ThroughputChannelExtensions`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `AssemblyInfo.cs` | — | 5 | Contains the assembly info implementation or configuration. |
| `ThroughputChannel.cs` | `ThroughputChannel` | 14 | Defines ThroughputChannel and its related behavior. |
| `ThroughputChannelConfiguration.cs` | `ThroughputChannelConfiguration` | 18 | Defines ThroughputChannelConfiguration and its related behavior. |
| `ThroughputChannelExtensions.cs` | `ThroughputChannelExtensions` | 21 | Defines ThroughputChannelExtensions and its related behavior. |
| `ThroughputChannelFeeder.cs` | `ThroughputChannelFeeder` | 80 | Defines ThroughputChannelFeeder and its related behavior. |
| `ThroughputChannelFeederConfiguration.cs` | `ThroughputChannelFeederConfiguration` | 18 | Defines ThroughputChannelFeederConfiguration and its related behavior. |
| `ThroughputChannelFeederMessage.cs` | `ThroughputChannelFeederMessage` | 46 | Defines ThroughputChannelFeederMessage and its related behavior. |
| `ThroughputChannelMetadata.cs` | `ThroughputChannelMetadata` | 23 | Defines ThroughputChannelMetadata and its related behavior. |
| `ThunderPropagator.Channels.Throughput.csproj` | — | 23 | Defines project build targets, dependencies, and package metadata. |

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`ThroughputChannelExtensions`](#throughputchannelextensions) | class | Represents the ThroughputChannelExtensions class. | — | `AddThroughputChannel(…)` |

### ThroughputChannelExtensions

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Throughput`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `AddThroughputChannel(…)`
- **Summary:** Represents the ThroughputChannelExtensions class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve ThroughputChannelExtensions from the configured service container or construct it with its declared dependencies.
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
  Current["Throughput"]
  Current --> T0["ThroughputChannelExtensions"]
```

The diagram shows the direct components documented by the **Throughput** area.

## Examples

Start with `ThroughputChannelExtensions` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)
- [Chat](../Chat/README.md)
- [Clock](../Clock/README.md)
- [NetworkMonitoring](../NetworkMonitoring/README.md)
- [Notifications](../Notifications/README.md)
- [ResourceMonitoring](../ResourceMonitoring/README.md)
- [TimeZones](../TimeZones/README.md)

[↑ Back to top](#contents)
