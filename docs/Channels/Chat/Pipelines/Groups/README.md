# Groups

## Contents

- [Overview](#overview)
- [Files](#files)
- [Package Dependencies](#package-dependencies)
- [Diagrams](#diagrams)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The **Groups** area organizes 8 direct sub-areas. Each child is documented separately so responsibilities and APIs remain easy to navigate.

## Files

*None.*

### Direct child areas

- [AddUser](./AddUser/README.md) `Types:2` `Files:2`
- [Create](./Create/README.md) `Types:3` `Files:3`
- [GetAll](./GetAll/README.md) `Types:2` `Files:2`
- [Join](./Join/README.md) `Types:2` `Files:2`
- [RemoveUser](./RemoveUser/README.md) `Types:2` `Files:2`
- [Rename](./Rename/README.md) `Types:3` `Files:3`
- [SetIcon](./SetIcon/README.md) `Types:3` `Files:3`
- [UserLeave](./UserLeave/README.md) `Types:2` `Files:2`

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
  Current["Groups"]
  Current --> C0["AddUser"]
  Current --> C1["Create"]
  Current --> C2["GetAll"]
  Current --> C3["Join"]
  Current --> C4["RemoveUser"]
  Current --> C5["Rename"]
  Current --> C6["SetIcon"]
  Current --> C7["UserLeave"]
```

The diagram shows the direct components documented by the **Groups** area.

## Examples

Choose the child area that matches the required capability; parent documentation intentionally does not duplicate child implementation details.

## See Also

- [Parent area](../README.md)
- [Messages](../Messages/README.md)
- [Users](../Users/README.md)

[↑ Back to top](#contents)
