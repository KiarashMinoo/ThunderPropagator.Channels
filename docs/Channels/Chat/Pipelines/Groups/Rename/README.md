# Rename

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

The **Rename** area groups 3 documented types, including `ChatChannelRenameGroupReceiverPipeline`, `ChatChannelRenameGroupReceiverPipelineRequestDto`, `ChatChannelRenameGroupReceiverPipelineResponseDto`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `ChatChannelRenameGroupReceiverPipeline.cs` | `ChatChannelRenameGroupReceiverPipeline` | 95 | Defines ChatChannelRenameGroupReceiverPipeline and its related behavior. |
| `ChatChannelRenameGroupReceiverPipelineRequestDto.cs` | `ChatChannelRenameGroupReceiverPipelineRequestDto` | 24 | Defines ChatChannelRenameGroupReceiverPipelineRequestDto and its related behavior. |
| `ChatChannelRenameGroupReceiverPipelineResponseDto.cs` | `ChatChannelRenameGroupReceiverPipelineResponseDto` | 14 | Defines ChatChannelRenameGroupReceiverPipelineResponseDto and its related behavior. |

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`ChatChannelRenameGroupReceiverPipeline`](#chatchannelrenamegroupreceiverpipeline) | class | Represents the ChatChannelRenameGroupReceiverPipeline class. | — | `RequestKey`, `Invoke(…)` |
| [`ChatChannelRenameGroupReceiverPipelineRequestDto`](#chatchannelrenamegroupreceiverpipelinerequestdto) | class | Represents the ChatChannelRenameGroupReceiverPipelineRequestDto class. | `BindingDictionary<string, object>, IRequestContentFormCollection` | — |
| [`ChatChannelRenameGroupReceiverPipelineResponseDto`](#chatchannelrenamegroupreceiverpipelineresponsedto) | class | Represents the ChatChannelRenameGroupReceiverPipelineResponseDto class. | `ResponseContentFormCollection` | `Group` |

### ChatChannelRenameGroupReceiverPipeline

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Chat.Pipelines.Groups.Rename`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `RequestKey`, `Invoke(…)`
- **Summary:** Represents the ChatChannelRenameGroupReceiverPipeline class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve ChatChannelRenameGroupReceiverPipeline from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### ChatChannelRenameGroupReceiverPipelineRequestDto

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Chat.Pipelines.Groups.Rename`
- **Inherits/implements:** `BindingDictionary<string, object>, IRequestContentFormCollection`
- **Attributes:** None detected
- **Key members:** Refer to the API surface in the source package
- **Summary:** Represents the ChatChannelRenameGroupReceiverPipelineRequestDto class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve ChatChannelRenameGroupReceiverPipelineRequestDto from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### ChatChannelRenameGroupReceiverPipelineResponseDto

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Chat.Pipelines.Groups.Rename`
- **Inherits/implements:** `ResponseContentFormCollection`
- **Attributes:** None detected
- **Key members:** `Group`
- **Summary:** Represents the ChatChannelRenameGroupReceiverPipelineResponseDto class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve ChatChannelRenameGroupReceiverPipelineResponseDto from the configured service container or construct it with its declared dependencies.
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
  Current["Rename"]
  Current --> T0["ChatChannelRenameGroupReceiverPipeline"]
  Current --> T1["ChatChannelRenameGroupReceiverPipelineRequestDto"]
  Current --> T2["ChatChannelRenameGroupReceiverPipelineResponseDto"]
```

The diagram shows the direct components documented by the **Rename** area.

## Examples

Start with `ChatChannelRenameGroupReceiverPipeline` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)
- [AddUser](../AddUser/README.md)
- [Create](../Create/README.md)
- [GetAll](../GetAll/README.md)
- [Join](../Join/README.md)
- [RemoveUser](../RemoveUser/README.md)
- [SetIcon](../SetIcon/README.md)
- [UserLeave](../UserLeave/README.md)

[↑ Back to top](#contents)
