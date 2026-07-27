# AddGame

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

The **AddGame** area groups 3 documented types, including `TicTacToeChannelAddGameReceiverPipeline`, `TicTacToeChannelAddGameReceiverPipelineRequestDto`, `TicTacToeChannelAddGameReceiverPipelineResponseDto`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `TicTacToeChannelAddGameReceiverPipeline.cs` | `TicTacToeChannelAddGameReceiverPipeline` | 73 | Defines TicTacToeChannelAddGameReceiverPipeline and its related behavior. |
| `TicTacToeChannelAddGameReceiverPipelineRequestDto.cs` | `TicTacToeChannelAddGameReceiverPipelineRequestDto` | 43 | Defines TicTacToeChannelAddGameReceiverPipelineRequestDto and its related behavior. |
| `TicTacToeChannelAddGameReceiverPipelineResponseDto.cs` | `TicTacToeChannelAddGameReceiverPipelineResponseDto` | 14 | Defines TicTacToeChannelAddGameReceiverPipelineResponseDto and its related behavior. |

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`TicTacToeChannelAddGameReceiverPipeline`](#tictactoechanneladdgamereceiverpipeline) | class | Represents the TicTacToeChannelAddGameReceiverPipeline class. | — | `RequestKey`, `Invoke(…)` |
| [`TicTacToeChannelAddGameReceiverPipelineRequestDto`](#tictactoechanneladdgamereceiverpipelinerequestdto) | class | Represents the TicTacToeChannelAddGameReceiverPipelineRequestDto class. | `BindingDictionary<string, object>, IRequestContentFormCollection` | — |
| [`TicTacToeChannelAddGameReceiverPipelineResponseDto`](#tictactoechanneladdgamereceiverpipelineresponsedto) | class | Represents the TicTacToeChannelAddGameReceiverPipelineResponseDto class. | `ResponseContentFormCollection` | `Subscription` |

### TicTacToeChannelAddGameReceiverPipeline

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Games.TicTacToe.Pipelines.AddGame`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `RequestKey`, `Invoke(…)`
- **Summary:** Represents the TicTacToeChannelAddGameReceiverPipeline class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve TicTacToeChannelAddGameReceiverPipeline from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### TicTacToeChannelAddGameReceiverPipelineRequestDto

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Games.TicTacToe.Pipelines.AddGame`
- **Inherits/implements:** `BindingDictionary<string, object>, IRequestContentFormCollection`
- **Attributes:** None detected
- **Key members:** Refer to the API surface in the source package
- **Summary:** Represents the TicTacToeChannelAddGameReceiverPipelineRequestDto class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve TicTacToeChannelAddGameReceiverPipelineRequestDto from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### TicTacToeChannelAddGameReceiverPipelineResponseDto

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Games.TicTacToe.Pipelines.AddGame`
- **Inherits/implements:** `ResponseContentFormCollection`
- **Attributes:** None detected
- **Key members:** `Subscription`
- **Summary:** Represents the TicTacToeChannelAddGameReceiverPipelineResponseDto class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve TicTacToeChannelAddGameReceiverPipelineResponseDto from the configured service container or construct it with its declared dependencies.
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
  Current["AddGame"]
  Current --> T0["TicTacToeChannelAddGameReceiverPipeline"]
  Current --> T1["TicTacToeChannelAddGameReceiverPipelineRequestDto"]
  Current --> T2["TicTacToeChannelAddGameReceiverPipelineResponseDto"]
```

The diagram shows the direct components documented by the **AddGame** area.

## Examples

Start with `TicTacToeChannelAddGameReceiverPipeline` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)
- [GetGames](../GetGames/README.md)
- [Move](../Move/README.md)
- [StartGame](../StartGame/README.md)

[↑ Back to top](#contents)
