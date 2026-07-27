# StartGame

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

The **StartGame** area groups 3 documented types, including `TicTacToeChannelStartGameReceiverPipeline`, `TicTacToeChannelStartGameReceiverPipelineRequestDto`, `TicTacToeChannelStartGameReceiverPipelineResponseDto`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `TicTacToeChannelStartGameReceiverPipeline.cs` | `TicTacToeChannelStartGameReceiverPipeline` | 70 | Defines TicTacToeChannelStartGameReceiverPipeline and its related behavior. |
| `TicTacToeChannelStartGameReceiverPipelineRequestDto.cs` | `TicTacToeChannelStartGameReceiverPipelineRequestDto` | 24 | Defines TicTacToeChannelStartGameReceiverPipelineRequestDto and its related behavior. |
| `TicTacToeChannelStartGameReceiverPipelineResponseDto.cs` | `TicTacToeChannelStartGameReceiverPipelineResponseDto` | 14 | Defines TicTacToeChannelStartGameReceiverPipelineResponseDto and its related behavior. |

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`TicTacToeChannelStartGameReceiverPipeline`](#tictactoechannelstartgamereceiverpipeline) | class | Represents the TicTacToeChannelStartGameReceiverPipeline class. | — | `RequestKey`, `Invoke(…)` |
| [`TicTacToeChannelStartGameReceiverPipelineRequestDto`](#tictactoechannelstartgamereceiverpipelinerequestdto) | class | Represents the TicTacToeChannelStartGameReceiverPipelineRequestDto class. | `BindingDictionary<string, object>, IRequestContentFormCollection` | — |
| [`TicTacToeChannelStartGameReceiverPipelineResponseDto`](#tictactoechannelstartgamereceiverpipelineresponsedto) | class | Represents the TicTacToeChannelStartGameReceiverPipelineResponseDto class. | `ResponseContentFormCollection` | `Subscription` |

### TicTacToeChannelStartGameReceiverPipeline

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Games.TicTacToe.Pipelines.StartGame`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `RequestKey`, `Invoke(…)`
- **Summary:** Represents the TicTacToeChannelStartGameReceiverPipeline class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve TicTacToeChannelStartGameReceiverPipeline from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### TicTacToeChannelStartGameReceiverPipelineRequestDto

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Games.TicTacToe.Pipelines.StartGame`
- **Inherits/implements:** `BindingDictionary<string, object>, IRequestContentFormCollection`
- **Attributes:** None detected
- **Key members:** Refer to the API surface in the source package
- **Summary:** Represents the TicTacToeChannelStartGameReceiverPipelineRequestDto class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve TicTacToeChannelStartGameReceiverPipelineRequestDto from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### TicTacToeChannelStartGameReceiverPipelineResponseDto

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Games.TicTacToe.Pipelines.StartGame`
- **Inherits/implements:** `ResponseContentFormCollection`
- **Attributes:** None detected
- **Key members:** `Subscription`
- **Summary:** Represents the TicTacToeChannelStartGameReceiverPipelineResponseDto class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve TicTacToeChannelStartGameReceiverPipelineResponseDto from the configured service container or construct it with its declared dependencies.
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
  Current["StartGame"]
  Current --> T0["TicTacToeChannelStartGameReceiverPipeline"]
  Current --> T1["TicTacToeChannelStartGameReceiverPipelineRequestDto"]
  Current --> T2["TicTacToeChannelStartGameReceiverPipelineResponseDto"]
```

The diagram shows the direct components documented by the **StartGame** area.

## Examples

Start with `TicTacToeChannelStartGameReceiverPipeline` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)
- [AddGame](../AddGame/README.md)
- [GetGames](../GetGames/README.md)
- [Move](../Move/README.md)

[↑ Back to top](#contents)
