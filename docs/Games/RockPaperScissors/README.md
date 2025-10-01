# Games RockPaperScissors

## Contents

- [Overview](#overview)
- [Files](#files)
- [Types & Members](#types--members)
- [Game Logic](#game-logic)
- [Configuration](#configuration)
- [RapidStreamer Dependencies](#rapidstreamer-dependencies)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The RockPaperScissors Game Channel provides a complete implementation of the classic Rock-Paper-Scissors game with real-time multiplayer capabilities. It supports both human and computer players, automatic matchmaking, and game state management through RapidStreamer's messaging system.

## Files

| File | Primary type(s) | LOC (approx) | Responsibility |
|------|----------------|--------------|----------------|
| RockPaperScissorsChannel.cs | RockPaperScissorsChannel | 30 | Core game channel with player matching |
| RockPaperScissorsChannelConfiguration.cs | RockPaperScissorsChannelConfiguration | 15 | Channel configuration settings |
| RockPaperScissorsChannelExtensions.cs | RockPaperScissorsChannelExtensions | 25 | Service registration extensions |
| RockPaperScissorsChannelFeederMessage.cs | RockPaperScissorsChannelFeederMessage | 40 | Game event message payload |
| RockPaperScissorsChannelMetadata.cs | RockPaperScissorsChannelMetadata | 20 | Channel metadata and program descriptors |
| RockPaperScissorsChannelReceiveEvent.cs | RockPaperScissorsChannelReceiveEvent | 30 | Game event handling |
| RockPaperScissorsComputer.cs | RockPaperScissorsComputer | 25 | Computer player AI logic |
| MoveKind.cs | MoveKind | 8 | Game move enumeration |
| Player.cs | Player | 30 | Player representation and state |
| PlayerType.cs | PlayerType | 8 | Player type enumeration |

## Types & Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|------|------|---------|-------------------|-------------|
| RockPaperScissorsChannel | Class | Core game management channel | AbstractChannel | PeekRandomPlayer, SendAsync |
| MoveKind | Enum | Game move types | - | Rock, Paper, Scissor |
| Player | Class | Player state and information | - | Subscription, Name, PlayerType, Move |
| PlayerType | Enum | Player types | - | Human, Computer |

### RockPaperScissorsChannel

**Key Methods**:
- `PeekRandomPlayer() : Subscription?` — Selects a random player from active subscriptions
- `SendAsync(Subscription, IReadOnlyDictionary<string, object?>, CancellationToken)` — Sends game messages to specific players

**Game Features**:
- Random player matching for multiplayer games
- Support for both human and computer players
- Real-time game event broadcasting

### MoveKind

**Values**:
- `Rock = 1` — Rock move (beats Scissor, loses to Paper)
- `Paper = 2` — Paper move (beats Rock, loses to Scissor)  
- `Scissor = 3` — Scissor move (beats Paper, loses to Rock)

### Player

**Key Properties**:
- `Subscription : Subscription?` — Player's channel subscription
- `Name : string` — Player display name
- `PlayerType : PlayerType` — Human or Computer player
- `Move : MoveKind` — Player's selected move

**Constructors**:
- `Player(Subscription)` — Creates player from subscription data
- `Player(string, PlayerType, MoveKind)` — Creates player with explicit values

## Game Logic

### Move Resolution
The classic Rock-Paper-Scissors rules apply:
- **Rock** beats **Scissor**
- **Paper** beats **Rock**
- **Scissor** beats **Paper**
- Same moves result in a tie

### Player Matching
- Random selection from active subscriptions
- Support for human vs human, human vs computer, and computer vs computer matches
- Real-time matchmaking as players join

### Game Flow
1. Players subscribe with their move selection
2. Channel matches players randomly
3. Game logic determines winner
4. Results broadcast to all participants

## Configuration

```csharp
services.AddRockPaperScissorsChannel(config => 
{
    config.IsEnabled = true;
});
```

## RapidStreamer Dependencies

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |

## Examples

### Playing Rock-Paper-Scissors

```csharp
// Subscribe as a human player
await channel.SubscribeAsync("player1", new Dictionary<string, string>
{
    [nameof(RockPaperScissorsChannelFeederMessage.PlayerName)] = "Alice",
    [nameof(RockPaperScissorsChannelFeederMessage.Move)] = MoveKind.Rock.ToString()
});

// Handle game results
await channel.SubscribeAsync("game-observer", message => 
{
    Console.WriteLine($"Game Result: {message.Player1Name} ({message.Player1Move}) vs {message.Player2Name} ({message.Player2Move})");
    Console.WriteLine($"Winner: {message.Winner}");
});
```

### Computer Player Integration

```csharp
var computerPlayer = new Player("Computer", PlayerType.Computer, MoveKind.Paper);
var humanPlayer = new Player("Human", PlayerType.Human, MoveKind.Rock);

// Game resolution logic
var winner = ResolveGame(humanPlayer, computerPlayer);
Console.WriteLine($"Winner: {winner.Name}");
```

## See Also

- [../TicTacToe/README.md](../TicTacToe/README.md) — Tic-tac-toe game implementation
- [../../Demo/README.md](../../Demo/README.md) — Demo applications

[↑ Back to top](#contents)