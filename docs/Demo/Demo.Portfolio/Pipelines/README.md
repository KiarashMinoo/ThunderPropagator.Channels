# Pipelines

## Contents

- [Overview](#overview)
- [Files](#files)
- [Types and Members](#types-and-members)
- [Package Dependencies](#package-dependencies)
- [Diagrams](#diagrams)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The **Pipelines** area groups 2 documented types, including `PortfolioDemoChannelBuyReceiverPipeline`, `PortfolioDemoChannelSellReceiverPipeline`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `PortfolioDemoChannelBuyReceiverPipeline.cs` | `PortfolioDemoChannelBuyReceiverPipeline` | 109 | Defines PortfolioDemoChannelBuyReceiverPipeline and its related behavior. |
| `PortfolioDemoChannelSellReceiverPipeline.cs` | `PortfolioDemoChannelSellReceiverPipeline` | 96 | Defines PortfolioDemoChannelSellReceiverPipeline and its related behavior. |

### Direct child areas

- [Dtos](./Dtos/README.md) `Types:2` `Files:2`

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`PortfolioDemoChannelBuyReceiverPipeline`](#portfoliodemochannelbuyreceiverpipeline) | class | Represents the PortfolioDemoChannelBuyReceiverPipeline class. | — | `RequestKey`, `Invoke(…)` |
| [`PortfolioDemoChannelSellReceiverPipeline`](#portfoliodemochannelsellreceiverpipeline) | class | Represents the PortfolioDemoChannelSellReceiverPipeline class. | — | `RequestKey`, `Invoke(…)` |

### PortfolioDemoChannelBuyReceiverPipeline

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Demo.Portfolio.Pipelines`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `RequestKey`, `Invoke(…)`
- **Summary:** Represents the PortfolioDemoChannelBuyReceiverPipeline class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve PortfolioDemoChannelBuyReceiverPipeline from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### PortfolioDemoChannelSellReceiverPipeline

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Demo.Portfolio.Pipelines`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `RequestKey`, `Invoke(…)`
- **Summary:** Represents the PortfolioDemoChannelSellReceiverPipeline class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve PortfolioDemoChannelSellReceiverPipeline from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

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
  Current["Pipelines"]
  Current --> C0["Dtos"]
```

The diagram shows the direct components documented by the **Pipelines** area.

## Examples

Start with `PortfolioDemoChannelBuyReceiverPipeline` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)

[↑ Back to top](#contents)
