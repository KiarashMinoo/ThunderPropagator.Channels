# Games

## Contents

- [Overview](#overview)
- [Game Implementations](#game-implementations)
- [Common Patterns](#common-patterns)
- [RapidStreamer Dependencies](#rapidstreamer-dependencies)
- [Getting Started](#getting-started)
- [See Also](#see-also)

## Overview

The Games collection demonstrates sophisticated real-time multiplayer game implementations using RapidStreamer channels. These examples showcase advanced patterns including session management, player matching, game state synchronization, and concurrent gameplay scenarios.

## Game Implementations

### [RockPaperScissors](./RockPaperScissors/README.md)
Classic rock-paper-scissors game featuring:
- Real-time multiplayer matchmaking
- Human vs computer player support
- Random player selection algorithms
- Game result broadcasting
- Simple game logic demonstration

### [TicTacToe](./TicTacToe/README.md)
Advanced tic-tac-toe implementation showcasing:
- Complex session management with concurrent games
- Thread-safe game state handling
- Custom subscription patterns
- Turn-based gameplay mechanics
- Advanced game logic and win condition detection

## Common Patterns

### Game Channel Architecture
All game channels follow sophisticated patterns for multiplayer gaming:

```csharp
[Unsubscribable] // Optional: Custom unsubscription handling
public class GameChannel : AbstractChannel<GameChannelMetadata, GameChannelConfiguration>
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = [];
    
    public GameChannel(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        // Game-specific initialization
    }
}
```

### Session Management
```csharp
private readonly ConcurrentDictionary<string, TicTacToeGame> _games = [];

private Subscription Subscribe(IConnectionInfo connectionInfo, string requestId, string sessionId, string playerName)
{
    // Custom subscription logic for game sessions
    var subscribeRequest = new GameChannelSubscribeRequest
    {
        SubscribingKeys = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            { nameof(GameMessage.SessionId), new Dictionary<string, string>
                {
                    { nameof(GameMessage.SessionId), sessionId },
                    { nameof(GameMessage.PlayerName), playerName }
                }
            }
        }
    };
    
    return Subscribe(connectionInfo, requestId, subscribeRequest).Single();
}
```

### Player Matching
```csharp
internal Subscription? PeekRandomPlayer()
{
    if (Subscriptions.SubscriptionCount <= 0)
        return null;
        
    var randomizedIndex = Random.Shared.Next(Subscriptions.SubscriptionCount);
    return Subscriptions.Subscriptions[randomizedIndex];
}
```

### Game State Broadcasting
```csharp
internal Task SendAsync(Subscription subscription, IReadOnlyDictionary<string, object?> gameState, CancellationToken cancellationToken = default)
{
    // Send game updates to specific players or all participants
    return base.SendAsync(subscription, gameState, false, 'N', cancellationToken);
}
```

## RapidStreamer Dependencies

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework with advanced channel features | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |

## Getting Started

### 1. Choose Your Game Type
Select based on complexity requirements:
- **Learning**: Start with RockPaperScissors for basic multiplayer patterns
- **Advanced**: Use TicTacToe for complex session management and state synchronization

### 2. Install and Register
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Simple game
    services.AddRockPaperScissorsChannel(config => 
    {
        config.IsEnabled = true;
    });
    
    // Advanced game with session management
    services.AddTicTacToeChannel(config => 
    {
        config.IsEnabled = true;
    });
}
```

### 3. Implement Game Logic
```csharp
public class GameService
{
    public async Task StartGameAsync(TicTacToeChannel channel)
    {
        // Create game session
        var subscription = await channel.SubscribeAsync(
            connectionInfo, 
            "request1", 
            "session123", 
            "Player1"
        );
        
        // Handle game events
        subscription.OnMessage(message => 
        {
            ProcessGameMove(message);
            BroadcastGameState(message);
        });
    }
}
```

### 4. Handle Multiplayer Scenarios
```csharp
// Multiple concurrent games
public class MultiGameManager
{
    private readonly Dictionary<string, GameSession> _activeSessions = [];
    
    public async Task<string> CreateGameSessionAsync()
    {
        var sessionId = Guid.NewGuid().ToString();
        var session = new GameSession(sessionId);
        
        _activeSessions[sessionId] = session;
        return sessionId;
    }
    
    public async Task JoinGameAsync(string sessionId, string playerName)
    {
        if (_activeSessions.TryGetValue(sessionId, out var session))
        {
            await session.AddPlayerAsync(playerName);
        }
    }
}
```

### 5. Implement Custom Game Rules
```csharp
public class CustomGameChannel : AbstractChannel<CustomGameMetadata, CustomGameConfiguration>
{
    protected override void OnSubscriptionAdded(Subscription subscription)
    {
        base.OnSubscriptionAdded(subscription);
        
        // Custom game initialization logic
        InitializePlayerInGame(subscription);
        CheckForGameStart();
    }
    
    private void InitializePlayerInGame(Subscription subscription)
    {
        // Extract player information from subscription
        var playerName = subscription.SubscribedPrograms.SubscribedKeys["PlayerName"];
        var gameMode = subscription.SubscribedPrograms.SubscribedKeys["GameMode"];
        
        // Initialize player in appropriate game session
        var session = FindOrCreateGameSession(gameMode);
        session.AddPlayer(playerName, subscription);
    }
}
```

## See Also

- [../Channels/README.md](../Channels/README.md) — Production-ready channels
- [../Demo/README.md](../Demo/README.md) — Business application demos

[↑ Back to top](#contents)