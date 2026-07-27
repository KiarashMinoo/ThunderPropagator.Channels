# Create

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

The **Create** area groups 3 documented types, including `ChatChannelCreateGroupReceiverPipeline`, `ChatChannelCreateGroupReceiverPipelineRequestDto`, `ChatChannelCreateGroupReceiverPipelineResponseDto`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `ChatChannelCreateGroupReceiverPipeline.cs` | `ChatChannelCreateGroupReceiverPipeline` | 65 | Defines ChatChannelCreateGroupReceiverPipeline and its related behavior. |
| `ChatChannelCreateGroupReceiverPipelineRequestDto.cs` | `ChatChannelCreateGroupReceiverPipelineRequestDto` | 24 | Defines ChatChannelCreateGroupReceiverPipelineRequestDto and its related behavior. |
| `ChatChannelCreateGroupReceiverPipelineResponseDto.cs` | `ChatChannelCreateGroupReceiverPipelineResponseDto` | 14 | Defines ChatChannelCreateGroupReceiverPipelineResponseDto and its related behavior. |

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`ChatChannelCreateGroupReceiverPipeline`](#chatchannelcreategroupreceiverpipeline) | class | Represents the ChatChannelCreateGroupReceiverPipeline class. | — | `RequestKey`, `Invoke(…)` |
| [`ChatChannelCreateGroupReceiverPipelineRequestDto`](#chatchannelcreategroupreceiverpipelinerequestdto) | class | Represents the ChatChannelCreateGroupReceiverPipelineRequestDto class. | `BindingDictionary<string, object>, IRequestContentFormCollection` | — |
| [`ChatChannelCreateGroupReceiverPipelineResponseDto`](#chatchannelcreategroupreceiverpipelineresponsedto) | class | Represents the ChatChannelCreateGroupReceiverPipelineResponseDto class. | `ResponseContentFormCollection` | `Group` |

### ChatChannelCreateGroupReceiverPipeline

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Chat.Pipelines.Groups.Create`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `RequestKey`, `Invoke(…)`
- **Summary:** Represents the ChatChannelCreateGroupReceiverPipeline class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve ChatChannelCreateGroupReceiverPipeline from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### ChatChannelCreateGroupReceiverPipelineRequestDto

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Chat.Pipelines.Groups.Create`
- **Inherits/implements:** `BindingDictionary<string, object>, IRequestContentFormCollection`
- **Attributes:** None detected
- **Key members:** Refer to the API surface in the source package
- **Summary:** Represents the ChatChannelCreateGroupReceiverPipelineRequestDto class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve ChatChannelCreateGroupReceiverPipelineRequestDto from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### ChatChannelCreateGroupReceiverPipelineResponseDto

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Chat.Pipelines.Groups.Create`
- **Inherits/implements:** `ResponseContentFormCollection`
- **Attributes:** None detected
- **Key members:** `Group`
- **Summary:** Represents the ChatChannelCreateGroupReceiverPipelineResponseDto class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve ChatChannelCreateGroupReceiverPipelineResponseDto from the configured service container or construct it with its declared dependencies.
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
  Current["Create"]
  Current --> T0["ChatChannelCreateGroupReceiverPipeline"]
  Current --> T1["ChatChannelCreateGroupReceiverPipelineRequestDto"]
  Current --> T2["ChatChannelCreateGroupReceiverPipelineResponseDto"]
```

The diagram shows the direct components documented by the **Create** area.

## Examples

Start with `ChatChannelCreateGroupReceiverPipeline` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)
- [AddUser](../AddUser/README.md)
- [GetAll](../GetAll/README.md)
- [Join](../Join/README.md)
- [RemoveUser](../RemoveUser/README.md)
- [Rename](../Rename/README.md)
- [SetIcon](../SetIcon/README.md)
- [UserLeave](../UserLeave/README.md)

[↑ Back to top](#contents)
