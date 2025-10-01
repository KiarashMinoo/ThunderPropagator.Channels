# Chat Channel

## Contents

- [Overview](#overview)
- [Files](#files)
- [Types & Members](#types--members)
- [Configuration](#configuration)
- [Serialization & Contracts](#serialization--contracts)
- [RapidStreamer Dependencies](#rapidstreamer-dependencies)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The Chat Channel provides real-time messaging capabilities for applications, supporting both direct user-to-user communication and group-based messaging. It features user authentication, group management, and message routing through a configurable entity framework context.

## Files

| File | Primary type(s) | LOC (approx) | Responsibility |
|------|----------------|--------------|----------------|
| ChatChannel.cs | ChatChannel | 22 | Core channel implementation managing logged-in users and subscriptions |
| ChatChannelConfiguration.cs | ChatChannelConfiguration | 15 | Channel configuration with enable/disable flag |
| ChatChannelExtensions.cs | ChatChannelExtensions | 60 | Service collection extensions for DI registration |
| ChatChannelFeederMessage.cs | ChatChannelFeederMessage | 50 | Message payload for chat events |
| ChatChannelMetadata.cs | ChatChannelMetadata | 20 | Channel metadata and program descriptors |
| Models/BaseChatContext.cs | BaseChatContext, IChatContext | 50 | Abstract database context for chat entities |

## Types & Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|------|------|---------|-------------------|-------------|
| ChatChannel | Class | Core channel managing chat functionality | AbstractChannel | LoggedInUsers, EmitMessage |
| ChatChannelConfiguration | Class | Configuration for chat channel behavior | AbstractChannelConfiguration | IsEnabled |
| ChatChannelExtensions | Static Class | Service registration extensions | - | AddChatChannel |
| ChatChannelFeederMessage | Class | Message payload for chat events | FeederMessage | UserId, SenderUserId, GroupId, Message, DateTime |
| ChatChannelMetadata | Class | Channel metadata and descriptors | AbstractChannelMetadata | ChannelProgramsDescriptors |
| BaseChatContext | Abstract Class | Database context abstraction | IChatContext | GetAsync, CreateAsync, UpdateAsync, DeleteAsync |

### ChatChannel

- **Kind**: Sealed class (in Release mode)
- **Namespace**: RapidStreamer.Channels.Chat
- **Inherits**: AbstractChannel<ChatChannelMetadata, ChatChannelConfiguration>

**Key Properties**:
- `LoggedInUsers : ConcurrentDictionary<string, Guid>` — Thread-safe collection of logged-in users by connection ID

**Key Methods**:
- `EmitMessage(ChatChannelFeederMessage)` — Broadcasts a message to subscribers
- `OnSubscriptionRemoved(Subscription)` — Cleans up logged-in users when subscription ends

**Thread-safety**: Uses ConcurrentDictionary for user tracking
**Usage Recipe**:
```csharp
services.AddChatChannel<MyChatContext>(config => {
    config.IsEnabled = true;
});
```

[↑ Back to top](#contents)

### ChatChannelFeederMessage

- **Kind**: Internal sealed class (in Release mode)
- **Namespace**: RapidStreamer.Channels.Chat
- **Inherits**: FeederMessage

**Key Properties**:
- `UserId : string` — Target user ID for the message
- `SenderUserId : Guid` — ID of the message sender
- `GroupId : Guid` — Group ID for group messages (Empty for direct messages)
- `Message : string` — Message content
- `DateTime : DateTimeOffset` — Message timestamp

**Constructors**:
- `ChatChannelFeederMessage()` — Default constructor
- `ChatChannelFeederMessage(Message)` — Internal constructor from Message entity

**Usage Recipe**:
```csharp
var message = new ChatChannelFeederMessage {
    UserId = "user123",
    SenderUserId = Guid.Parse("..."),
    Message = "Hello world!"
};
```

[↑ Back to top](#contents)

### BaseChatContext

- **Kind**: Abstract class
- **Namespace**: RapidStreamer.Channels.Chat.Models
- **Implements**: IChatContext

**Key Methods**:
- `GetAsync<TEntity>(Expression<Func<TEntity, bool>>)` — Get single entity by predicate
- `GetAsync<TEntity, TPk>(TPk)` — Get entity by primary key
- `GetAllAsync<TEntity>(Expression<Func<TEntity, bool>>)` — Get all entities matching predicate
- `CreateAsync<TEntity>(TEntity)` — Create new entity
- `UpdateAsync<TEntity>(TEntity)` — Update existing entity
- `DeleteAsync<TEntity, TPk>(TPk)` — Delete entity by primary key

**Abstract Methods**:
- `Migrate()` — Database migration logic
- `Seed()` — Database seeding logic

**Thread-safety**: Uses lock for initialization
**Usage Recipe**:
```csharp
public class MyChatContext : BaseChatContext {
    protected override void Migrate() { /* EF migrations */ }
    protected override void Seed() { /* Initial data */ }
    public override async Task<TEntity?> GetAsync<TEntity>(/* ... */) { /* Implementation */ }
}
```

[↑ Back to top](#contents)

## Configuration

The chat channel is configured through `ChatChannelConfiguration`:

- `IsEnabled : bool` — Controls whether the channel accepts subscriptions (default: true)

## Serialization & Contracts

Message contracts follow the FeederMessage pattern:
- All properties use GetValueOrDefault/SetValue pattern for serialization
- DateTime values use DateTimeOffset for timezone awareness
- Guid values default to Guid.Empty for optional fields

## RapidStreamer Dependencies

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |

## Examples

### Basic Chat Channel Setup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddChatChannel<EntityFrameworkChatContext>(config => 
    {
        config.IsEnabled = true;
    });
}
```

### Custom Chat Context Implementation

```csharp
public class SqlServerChatContext : BaseChatContext
{
    private readonly DbContext _dbContext;
    
    public SqlServerChatContext(DbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    protected override void Migrate()
    {
        _dbContext.Database.Migrate();
    }
    
    protected override void Seed()
    {
        // Add default groups, users, etc.
    }
    
    public override async Task<TEntity?> GetAsync<TEntity>(
        Expression<Func<TEntity, bool>> expression, 
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<TEntity>()
            .FirstOrDefaultAsync(expression, cancellationToken);
    }
}
```

## See Also

- [../Clock/README.md](../Clock/README.md) — Time-based messaging
- [../Notifications/README.md](../Notifications/README.md) — User notifications

[↑ Back to top](#contents)