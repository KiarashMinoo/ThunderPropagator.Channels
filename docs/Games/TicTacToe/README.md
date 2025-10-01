# Games TicTacToe

## Contents

- [Overview](#overview)
- [Files](#files)
- [Types & Members](#types--members)
- [Game Logic](#game-logic)
- [Configuration](#configuration)
- [Performance Notes](#performance-notes)
- [RapidStreamer Dependencies](#rapidstreamer-dependencies)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The TicTacToe Game Channel provides a complete implementation of the classic Tic-tac-toe game with advanced features including session management, concurrent game support, player management, and real-time game state synchronization. It demonstrates complex state management and multiplayer gaming patterns using RapidStreamer.

## Files

| File | Primary type(s) | LOC (approx) | Responsibility |
|------|----------------|--------------|----------------|
| TicTacToeChannel.cs | TicTacToeChannel | 143 | Advanced channel with session and game management |
| TicTacToeChannelConfiguration.cs | TicTacToeChannelConfiguration | 15 | Channel configuration settings |
| TicTacToeChannelExtensions.cs | TicTacToeChannelExtensions | 25 | Service registration extensions |
| TicTacToeChannelFeederMessage.cs | TicTacToeChannelFeederMessage | 45 | Game state message payload |
| TicTacToeChannelMetadata.cs | TicTacToeChannelMetadata | 25 | Channel metadata and program descriptors |
| TicTacToeChannelSubscribeRequest.cs | TicTacToeChannelSubscribeRequest | 20 | Custom subscription request handling |
| Game/ | Various | 200+ | Game logic, players, and state management |
| Pipelines/ | Various | 100+ | Game event processing pipelines |

## Types & Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|------|------|---------|-------------------|-------------|
| TicTacToeChannel | Class | Advanced game management channel | AbstractChannel | Subscribe, _games |
| TicTacToeChannelSubscribeRequest | Class | Custom subscription handling | - | SubscribingKeys, SubscribingFields |

### TicTacToeChannel

**Attributes**:
- `[Unsubscribable]` — Special handling for unsubscription

**Key Properties**:
- `_games : ConcurrentDictionary<string, TicTacToeGame>` — Thread-safe game session storage

**Key Methods**:
- `Subscribe(IConnectionInfo, string, string, string) : Subscription` — Custom subscription with session and player management

**Advanced Features**:
- **Session Management**: Multiple concurrent game sessions
- **Thread Safety**: ConcurrentDictionary for game state
- **Custom Subscription**: Specialized subscription handling
- **Game State Sync**: Real-time board state updates

### TicTacToeChannelSubscribeRequest

**Key Properties**:
- `SubscribingKeys : Dictionary<string, IReadOnlyDictionary<string, string>>` — Session and player key mapping
- `SubscribingFields : HashSet<string>` — Game state fields (Row, Column, Sign)
- `SubscriptionMode : SubscriptionMode` — Full subscription mode

## Game Logic

### Game Board
- 3x3 grid with row/column coordinates
- X and O player signs
- Win condition detection (rows, columns, diagonals)
- Draw condition handling

### Session Management
- Unique session IDs for concurrent games
- Player name registration per session
- Game state persistence across moves
- Thread-safe game operations

### Move Validation
- Turn-based gameplay enforcement
- Valid position checking
- Game completion detection
- Invalid move handling

## Configuration

```csharp
services.AddTicTacToeChannel(config => 
{
    config.IsEnabled = true;
});
```

## Performance Notes

- **Concurrent Games**: ConcurrentDictionary enables multiple simultaneous games
- **Memory Management**: Games cleaned up after completion
- **Thread Safety**: All game operations are thread-safe
- **Real-time Updates**: Immediate board state synchronization

## RapidStreamer Dependencies

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |

## Examples

### Starting a Tic-tac-toe Game

```csharp
// Subscribe to a game session
var subscription = await channel.SubscribeAsync(connectionInfo, "request1", "session123", "PlayerX");

// Handle game updates
subscription.OnMessage(message => 
{
    Console.WriteLine($"Move: Player {message.Sign} at ({message.Row}, {message.Column})");
    UpdateGameBoard(message.Row, message.Column, message.Sign);
});
```

### Game State Monitoring

```csharp
await channel.SubscribeAsync("game-monitor", message => 
{
    if (message.GameCompleted)
    {
        Console.WriteLine($"Game {message.SessionId} completed!");
        Console.WriteLine($"Winner: {message.Winner ?? "Draw"}");
    }
    else
    {
        Console.WriteLine($"Current turn: {message.CurrentPlayer}");
        DisplayBoard(message.BoardState);
    }
});
```

### Multi-Session Support

```csharp
// Multiple concurrent games
var game1 = await channel.SubscribeAsync(connectionInfo, "req1", "session1", "Alice");
var game2 = await channel.SubscribeAsync(connectionInfo, "req2", "session2", "Bob");

// Each game maintains independent state
// Games run concurrently without interference
```

## See Also

- [../RockPaperScissors/README.md](../RockPaperScissors/README.md) — Rock-paper-scissors game implementation
- [../../Demo/README.md](../../Demo/README.md) — Demo applications

[↑ Back to top](#contents)