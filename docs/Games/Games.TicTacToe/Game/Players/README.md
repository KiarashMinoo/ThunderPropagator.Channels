# Players

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

The **Players** area groups 3 documented types, including `ComputerPlayer`, `HumanPlayer`, `Player`. It provides the contracts and implementation used by this part of ThunderPropagator.Channels.

## Files

| File | Primary type(s)/symbol(s) | LOC (approx.) | Responsibility |
|---|---|---:|---|
| `ComputerPlayer.cs` | `ComputerPlayer` | 155 | Defines ComputerPlayer and its related behavior. |
| `HumanPlayer.cs` | `HumanPlayer` | 30 | Defines HumanPlayer and its related behavior. |
| `Player.cs` | `Player` | 36 | Defines Player and its related behavior. |

## Types and Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|---|---|---|---|---|
| [`ComputerPlayer`](#computerplayer) | class | Represents the ComputerPlayer class. | `Player` | `Kind`, `SetTicTacToeGame(…)` |
| [`HumanPlayer`](#humanplayer) | class | Represents the HumanPlayer class. | `Player` | `Kind`, `HumanMove(…)` |
| [`Player`](#player) | class | Represents the Player class. | — | `Kind`, `Sign`, `Name`, `ConnectionId`, `SetTicTacToeGame(…)`, `OnBeforePlayerMovedHandler(…)` |

### ComputerPlayer

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Games.TicTacToe.Game.Players`
- **Inherits/implements:** `Player`
- **Attributes:** None detected
- **Key members:** `Kind`, `SetTicTacToeGame(…)`
- **Summary:** Represents the ComputerPlayer class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve ComputerPlayer from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### HumanPlayer

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Games.TicTacToe.Game.Players`
- **Inherits/implements:** `Player`
- **Attributes:** None detected
- **Key members:** `Kind`, `HumanMove(…)`
- **Summary:** Represents the HumanPlayer class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve HumanPlayer from the configured service container or construct it with its declared dependencies.
```

[↑ Back to top](#contents)

### Player

- **Kind:** class
- **Namespace:** `ThunderPropagator.Channels.Games.TicTacToe.Game.Players`
- **Inherits/implements:** None declared
- **Attributes:** None detected
- **Key members:** `Kind`, `Sign`, `Name`, `ConnectionId`, `SetTicTacToeGame(…)`, `OnBeforePlayerMovedHandler(…)`, `OnPlayerMoved(…)`, `NotifyIsWon(…)`
- **Summary:** Represents the Player class.
- **Thread safety:** Follow the lifetime and concurrency guarantees of the owning component; no additional guarantee is inferred.

**Usage recipe**

```csharp
// Resolve Player from the configured service container or construct it with its declared dependencies.
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
  Current["Players"]
  Current --> T0["ComputerPlayer"]
  Current --> T1["HumanPlayer"]
  Current --> T2["Player"]
```

The diagram shows the direct components documented by the **Players** area.

## Examples

Start with `ComputerPlayer` as the primary entry point for this folder, then follow its linked contracts and collaborators.

## See Also

- [Parent area](../README.md)
- [Enums](../Enums/README.md)
- [Exceptions](../Exceptions/README.md)

[↑ Back to top](#contents)
