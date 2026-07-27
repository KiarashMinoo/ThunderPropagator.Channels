# Messages

## Contents

- [Overview](#overview)
- [Files](#files)
- [Types and Members](#types-and-members)
- [Validation and Constraints](#validation-and-constraints)
- [Package Dependencies](#package-dependencies)
- [Diagrams](#diagrams)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The **Messages** area groups 2 documented types, including `Message`, `MessageService`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `Message.cs` | `Message` | 47 | Defines Message and its related behavior. |
| `MessageService.cs` | `MessageService` | 36 | Defines MessageService and its related behavior. |

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`Message`](#message) | class | Represents the Message class. | — | `Id`, `SenderId`, `Sender`, `ReceiverId`, `Receiver`, `GroupId` |
| [`MessageService`](#messageservice) | class | Represents the MessageService class. | — | `SendMessageAsync(…)`, `SendMessageToGroupAsync(…)` |

### Message

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Chat.Models.Messages`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `Id`, `SenderId`, `Sender`, `ReceiverId`, `Receiver`, `GroupId`, `Group`, `Created`
- **Summary:** Represents the Message class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve Message from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### MessageService

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Chat.Models.Messages`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `SendMessageAsync(…)`, `SendMessageToGroupAsync(…)`
- **Summary:** Represents the MessageService class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve MessageService from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

## Validation and Constraints

Inputs are validated at component boundaries. Callers should provide non-null required values and handle domain or argument exceptions without retrying invalid requests unchanged.

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
  Current["Messages"]
  Current --> T0["Message"]
  Current --> T1["MessageService"]
```

The diagram shows the direct components documented by the **Messages** area.

## Examples

Start with `Message` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)
- [Groups](../Groups/README.md)
- [Users](../Users/README.md)

[↑ Back to top](#contents)
