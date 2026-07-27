# WeatherApi

## Contents

- [Overview](#overview)
- [Files](#files)
- [Types and Members](#types-and-members)
- [Serialization and Contracts](#serialization-and-contracts)
- [Package Dependencies](#package-dependencies)
- [Diagrams](#diagrams)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The **WeatherApi** area groups 1 documented type, including `CachedWeatherHandler`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `CachedWeatherHandler.cs` | `CachedWeatherHandler` | 35 | Defines CachedWeatherHandler and its related behavior. |
| `WeatherApiService.cs` | `WeatherApiService` | 77 | Defines WeatherApiService and its related behavior. |

### Direct child areas

- [Models](./Models/README.md) `Types:2` `Files:4`

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`CachedWeatherHandler`](#cachedweatherhandler) | class | Represents the CachedWeatherHandler class. | `DelegatingHandler` | `SendAsync(…)` |

### CachedWeatherHandler

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.TimeZones.WeatherApi`
- **Inherits/implements:** `DelegatingHandler`
- **Attributes:** None detected
- **Key members:** `SendAsync(…)`
- **Summary:** Represents the CachedWeatherHandler class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve CachedWeatherHandler from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

## Serialization and Contracts

Serialization behavior is part of the public wire or persistence contract in this area. Preserve field names, ordering rules, content negotiation, and backward-compatibility expectations when changing these types.

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
  Current["WeatherApi"]
  Current --> C0["Models"]
```

The diagram shows the direct components documented by the **WeatherApi** area.

## Examples

Start with `CachedWeatherHandler` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)

[↑ Back to top](#contents)
