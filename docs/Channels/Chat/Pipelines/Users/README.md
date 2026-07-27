# Users

## Contents

- [Overview](#overview)
- [Files](#files)
- [Package Dependencies](#package-dependencies)
- [Diagrams](#diagrams)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The **Users** area organizes 5 direct sub-areas. Each child is documented separately so responsibilities and APIs remain easy to navigate.

## Files

*None.*

### Direct child areas

- [Login](./Login/README.md) `Types:4` `Files:4`
- [Register](./Register/README.md) `Types:3` `Files:3`
- [SetAvatar](./SetAvatar/README.md) `Types:2` `Files:2`
- [SetName](./SetName/README.md) `Types:2` `Files:2`
- [Update](./Update/README.md) `Types:3` `Files:3`

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
  Current["Users"]
  Current --> C0["Login"]
  Current --> C1["Register"]
  Current --> C2["SetAvatar"]
  Current --> C3["SetName"]
  Current --> C4["Update"]
```

The diagram shows the direct components documented by the **Users** area.

## Examples

Choose the child area that matches the required capability; parent documentation intentionally does not duplicate child implementation details.

## See Also

- [Parent area](../README.md)
- [Groups](../Groups/README.md)
- [Messages](../Messages/README.md)

[↑ Back to top](#contents)
