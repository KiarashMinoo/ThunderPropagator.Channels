# Pipelines

## Contents

- [Overview](#overview)
- [Files](#files)
- [Package Dependencies](#package-dependencies)
- [Diagrams](#diagrams)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The **Pipelines** area organizes 4 direct sub-areas. Each child is documented separately so responsibilities and APIs remain easy to navigate.

## Files

*None.*

### Direct child areas

- [AddGame](./AddGame/README.md) `Types:3` `Files:3`
- [GetGames](./GetGames/README.md) `Types:3` `Files:2`
- [Move](./Move/README.md) `Types:3` `Files:3`
- [StartGame](./StartGame/README.md) `Types:3` `Files:3`

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
  Current --> C0["AddGame"]
  Current --> C1["GetGames"]
  Current --> C2["Move"]
  Current --> C3["StartGame"]
```

The diagram shows the direct components documented by the **Pipelines** area.

## Examples

Choose the child area that matches the required capability; parent documentation intentionally does not duplicate child implementation details.

## See Also

- [Parent area](../README.md)
- [Game](../Game/README.md)

[↑ Back to top](#contents)
