# Move

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

The **Move** area groups 3 documented types, including `TicTacToeChannelMoveReceiverPipeline`, `TicTacToeChannelMoveReceiverPipelineRequestDto`, `TicTacToeChannelMoveReceiverPipelineResponseDto`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `TicTacToeChannelMoveReceiverPipeline.cs` | `TicTacToeChannelMoveReceiverPipeline` | 67 | Defines TicTacToeChannelMoveReceiverPipeline and its related behavior. |
| `TicTacToeChannelMoveReceiverPipelineRequestDto.cs` | `TicTacToeChannelMoveReceiverPipelineRequestDto` | 30 | Defines TicTacToeChannelMoveReceiverPipelineRequestDto and its related behavior. |
| `TicTacToeChannelMoveReceiverPipelineResponseDto.cs` | `TicTacToeChannelMoveReceiverPipelineResponseDto` | 12 | Defines TicTacToeChannelMoveReceiverPipelineResponseDto and its related behavior. |

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`TicTacToeChannelMoveReceiverPipeline`](#tictactoechannelmovereceiverpipeline) | class | Represents the TicTacToeChannelMoveReceiverPipeline class. | — | `RequestKey`, `Invoke(…)` |
| [`TicTacToeChannelMoveReceiverPipelineRequestDto`](#tictactoechannelmovereceiverpipelinerequestdto) | class | Represents the TicTacToeChannelMoveReceiverPipelineRequestDto class. | `BindingDictionary<string, object>, IRequestContentFormCollection` | — |
| [`TicTacToeChannelMoveReceiverPipelineResponseDto`](#tictactoechannelmovereceiverpipelineresponsedto) | class | Represents the TicTacToeChannelMoveReceiverPipelineResponseDto class. | `ResponseContentFormCollection` | — |

### TicTacToeChannelMoveReceiverPipeline

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Games.TicTacToe.Pipelines.Move`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `RequestKey`, `Invoke(…)`
- **Summary:** Represents the TicTacToeChannelMoveReceiverPipeline class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve TicTacToeChannelMoveReceiverPipeline from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### TicTacToeChannelMoveReceiverPipelineRequestDto

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Games.TicTacToe.Pipelines.Move`
- **Inherits/implements:** `BindingDictionary<string, object>, IRequestContentFormCollection`
- **Attributes:** None detected
- **Key members:** Refer to the API surface in the source package
- **Summary:** Represents the TicTacToeChannelMoveReceiverPipelineRequestDto class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve TicTacToeChannelMoveReceiverPipelineRequestDto from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### TicTacToeChannelMoveReceiverPipelineResponseDto

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Games.TicTacToe.Pipelines.Move`
- **Inherits/implements:** `ResponseContentFormCollection`
- **Attributes:** None detected
- **Key members:** Refer to the API surface in the source package
- **Summary:** Represents the TicTacToeChannelMoveReceiverPipelineResponseDto class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve TicTacToeChannelMoveReceiverPipelineResponseDto from the configured service container or construct it with its declared dependencies.
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
  Current["Move"]
  Current --> T0["TicTacToeChannelMoveReceiverPipeline"]
  Current --> T1["TicTacToeChannelMoveReceiverPipelineRequestDto"]
  Current --> T2["TicTacToeChannelMoveReceiverPipelineResponseDto"]
```

The diagram shows the direct components documented by the **Move** area.

## Examples

Start with `TicTacToeChannelMoveReceiverPipeline` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)
- [AddGame](../AddGame/README.md)
- [GetGames](../GetGames/README.md)
- [StartGame](../StartGame/README.md)

[↑ Back to top](#contents)
