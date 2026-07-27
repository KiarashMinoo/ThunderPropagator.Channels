# Send

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

The **Send** area groups 2 documented types, including `ChatChannelSendMessageReceiverPipeline`, `ChatChannelSendMessageReceiverPipelineRequestDto`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `ChatChannelSendMessageReceiverPipeline.cs` | `ChatChannelSendMessageReceiverPipeline` | 85 | Defines ChatChannelSendMessageReceiverPipeline and its related behavior. |
| `ChatChannelSendMessageReceiverPipelineRequestDto.cs` | `ChatChannelSendMessageReceiverPipelineRequestDto` | 30 | Defines ChatChannelSendMessageReceiverPipelineRequestDto and its related behavior. |

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`ChatChannelSendMessageReceiverPipeline`](#chatchannelsendmessagereceiverpipeline) | class | Represents the ChatChannelSendMessageReceiverPipeline class. | — | `RequestKey`, `Invoke(…)` |
| [`ChatChannelSendMessageReceiverPipelineRequestDto`](#chatchannelsendmessagereceiverpipelinerequestdto) | class | Represents the ChatChannelSendMessageReceiverPipelineRequestDto class. | `BindingDictionary<string, object?>, IRequestContentFormCollection` | — |

### ChatChannelSendMessageReceiverPipeline

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Chat.Pipelines.Messages.Send`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `RequestKey`, `Invoke(…)`
- **Summary:** Represents the ChatChannelSendMessageReceiverPipeline class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve ChatChannelSendMessageReceiverPipeline from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### ChatChannelSendMessageReceiverPipelineRequestDto

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Chat.Pipelines.Messages.Send`
- **Inherits/implements:** `BindingDictionary<string, object?>, IRequestContentFormCollection`
- **Attributes:** None detected
- **Key members:** Refer to the API surface in the source package
- **Summary:** Represents the ChatChannelSendMessageReceiverPipelineRequestDto class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve ChatChannelSendMessageReceiverPipelineRequestDto from the configured service container or construct it with its declared dependencies.
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
  Current["Send"]
  Current --> T0["ChatChannelSendMessageReceiverPipeline"]
  Current --> T1["ChatChannelSendMessageReceiverPipelineRequestDto"]
```

The diagram shows the direct components documented by the **Send** area.

## Examples

Start with `ChatChannelSendMessageReceiverPipeline` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)

[↑ Back to top](#contents)
