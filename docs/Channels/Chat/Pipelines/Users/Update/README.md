# Update

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

The **Update** area groups 3 documented types, including `ChatChannelUpdateUserReceiverPipeline`, `ChatChannelUpdateUserReceiverPipelineRequestDto`, `ChatChannelUpdateUserReceiverPipelineResponseDto`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `ChatChannelUpdateUserReceiverPipeline.cs` | `ChatChannelUpdateUserReceiverPipeline` | 78 | Defines ChatChannelUpdateUserReceiverPipeline and its related behavior. |
| `ChatChannelUpdateUserReceiverPipelineRequestDto.cs` | `ChatChannelUpdateUserReceiverPipelineRequestDto` | 24 | Defines ChatChannelUpdateUserReceiverPipelineRequestDto and its related behavior. |
| `ChatChannelUpdateUserReceiverPipelineResponseDto.cs` | `ChatChannelUpdateUserReceiverPipelineResponseDto` | 15 | Defines ChatChannelUpdateUserReceiverPipelineResponseDto and its related behavior. |

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`ChatChannelUpdateUserReceiverPipeline`](#chatchannelupdateuserreceiverpipeline) | class | Represents the ChatChannelUpdateUserReceiverPipeline class. | — | `RequestKey`, `Invoke(…)` |
| [`ChatChannelUpdateUserReceiverPipelineRequestDto`](#chatchannelupdateuserreceiverpipelinerequestdto) | class | Represents the ChatChannelUpdateUserReceiverPipelineRequestDto class. | `BindingDictionary<string, object>, IRequestContentFormCollection` | — |
| [`ChatChannelUpdateUserReceiverPipelineResponseDto`](#chatchannelupdateuserreceiverpipelineresponsedto) | class | Represents the ChatChannelUpdateUserReceiverPipelineResponseDto class. | `ResponseContentFormCollection` | `User` |

### ChatChannelUpdateUserReceiverPipeline

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Chat.Pipelines.Users.Update`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `RequestKey`, `Invoke(…)`
- **Summary:** Represents the ChatChannelUpdateUserReceiverPipeline class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve ChatChannelUpdateUserReceiverPipeline from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### ChatChannelUpdateUserReceiverPipelineRequestDto

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Chat.Pipelines.Users.Update`
- **Inherits/implements:** `BindingDictionary<string, object>, IRequestContentFormCollection`
- **Attributes:** None detected
- **Key members:** Refer to the API surface in the source package
- **Summary:** Represents the ChatChannelUpdateUserReceiverPipelineRequestDto class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve ChatChannelUpdateUserReceiverPipelineRequestDto from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### ChatChannelUpdateUserReceiverPipelineResponseDto

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Chat.Pipelines.Users.Update`
- **Inherits/implements:** `ResponseContentFormCollection`
- **Attributes:** None detected
- **Key members:** `User`
- **Summary:** Represents the ChatChannelUpdateUserReceiverPipelineResponseDto class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve ChatChannelUpdateUserReceiverPipelineResponseDto from the configured service container or construct it with its declared dependencies.
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
  Current["Update"]
  Current --> T0["ChatChannelUpdateUserReceiverPipeline"]
  Current --> T1["ChatChannelUpdateUserReceiverPipelineRequestDto"]
  Current --> T2["ChatChannelUpdateUserReceiverPipelineResponseDto"]
```

The diagram shows the direct components documented by the **Update** area.

## Examples

Start with `ChatChannelUpdateUserReceiverPipeline` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)
- [Login](../Login/README.md)
- [Register](../Register/README.md)
- [SetAvatar](../SetAvatar/README.md)
- [SetName](../SetName/README.md)

[↑ Back to top](#contents)
