# Notifications Channel

## Contents

- [Overview](#overview)
- [Files](#files)
- [Types & Members](#types--members)
- [Configuration](#configuration)
- [Serialization & Contracts](#serialization--contracts)
- [Performance Notes](#performance-notes)
- [RapidStreamer Dependencies](#rapidstreamer-dependencies)
- [Examples](#examples)
- [See Also](#see-also)

## Overview

The Notifications Channel provides a sophisticated notification delivery system with support for user targeting, priority levels, notification types, and snapshot-based persistence. It handles both broadcast and targeted notifications with automatic deduplication and state management for read/unread status.

## Files

| File | Primary type(s) | LOC (approx) | Responsibility |
|------|----------------|--------------|----------------|
| NotificationsChannel.cs | NotificationsChannel | 75 | Core channel with snapshot and user targeting logic |
| NotificationsChannelFeederMessage.cs | NotificationsChannelFeederMessage | 100 | Comprehensive notification message payload |
| NotificationsChannelMetadata.cs | NotificationsChannelMetadata | 25 | Channel metadata and program descriptors |
| NotificationsExtensions.cs | NotificationsExtensions | 50 | Flexible service registration extensions |
| NotificationsFeederConfiguration.cs | NotificationsFeederConfiguration | 10 | Abstract base for feeder configurations |
| NotificationPriority.cs | NotificationPriority | 10 | Enumeration for notification priority levels |
| NotificationType.cs | NotificationType | 8 | Enumeration for notification content types |

## Types & Members

| Type | Kind | Summary | Inherits/Implements | Key Members |
|------|------|---------|-------------------|-------------|
| NotificationsChannel | Generic Class | Core notification delivery channel | AbstractChannel | OnSubscriptionAdded, EmitMessage, SnapshotsToSendAsync |
| NotificationsChannelFeederMessage | Class | Comprehensive notification payload | FeederMessage | UserId, Id, Subject, Body, Priority, Type, Seen |
| NotificationsExtensions | Static Class | Service registration extensions | - | AddNotificationsChannel, AddNotificationsChannelFeeder |
| NotificationPriority | Enum | Priority levels for notifications | - | VeryLow, Low, Normal, High, VeryHigh |
| NotificationType | Enum | Content format types | - | Text, Html |

### NotificationsChannel<TNotificationsChannelConfiguration>

- **Kind**: Sealed generic class (in Release mode)
- **Namespace**: RapidStreamer.Channels.Notifications
- **Inherits**: AbstractChannel<NotificationsChannelMetadata<TNotificationsChannelConfiguration>, TNotificationsChannelConfiguration>
- **Constraints**: TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()

**Key Properties**:
- `_cancellationToken : CancellationToken` — Application stopping token

**Key Methods**:
- `OnSubscriptionAdded(Subscription)` — Sends historical notifications to new subscribers
- `EmitMessage(FeederMessage)` — Handles broadcast and targeted message delivery
- `SnapshotsToSendAsync(Subscription, CancellationToken)` — Retrieves user-specific notification snapshots

**Thread-safety**: Uses snapshot search for concurrent access
**Usage Recipe**:
```csharp
services.AddNotificationsChannel<MyNotificationConfiguration>(config => {
    config.IsEnabled = true;
});
```

[↑ Back to top](#contents)

### NotificationsChannelFeederMessage

- **Kind**: Sealed class (in Release mode)
- **Namespace**: RapidStreamer.Channels.Notifications
- **Inherits**: FeederMessage

**Key Properties**:
- `UserId : string?` — Target user ID (null for broadcast)
- `Date : DateTime` — Notification date
- `Time : TimeSpan` — Notification time
- `Id : string` — Unique notification identifier
- `Origin : string` — Source system or component
- `Type : NotificationType` — Content format (Text/Html)
- `Priority : NotificationPriority` — Priority level (-2 to 2)
- `Icon : string` — Icon identifier or URL
- `Subject : string` — Notification title/subject
- `Body : string` — Full notification content
- `EllipsisBody : string` — Truncated content for previews
- `Seen : int` — Read status (0 = unread, 1 = read)
- `Metadata : string` — Additional JSON metadata

**Constructors**:
- `NotificationsChannelFeederMessage()` — Default constructor
- `NotificationsChannelFeederMessage(IDictionary<string, object?>)` — Internal constructor from dictionary

**Usage Recipe**:
```csharp
var notification = new NotificationsChannelFeederMessage
{
    UserId = "user123",
    Id = Guid.NewGuid().ToString(),
    Subject = "New Message",
    Body = "You have received a new message",
    Priority = NotificationPriority.High,
    Type = NotificationType.Text
};
```

[↑ Back to top](#contents)

### NotificationPriority

- **Kind**: Public enum
- **Namespace**: RapidStreamer.Channels.Notifications

**Values**:
- `VeryLow = -2` — Lowest priority notifications
- `Low = -1` — Low priority notifications  
- `Normal = 0` — Standard priority (default)
- `High = 1` — High priority notifications
- `VeryHigh = 2` — Highest priority notifications

**Usage Recipe**:
```csharp
notification.Priority = NotificationPriority.VeryHigh; // Critical alerts
notification.Priority = NotificationPriority.Low;      // Background info
```

[↑ Back to top](#contents)

### NotificationType

- **Kind**: Public enum
- **Namespace**: RapidStreamer.Channels.Notifications

**Values**:
- `Text = 0` — Plain text content (default)
- `Html = 1` — HTML formatted content

**Usage Recipe**:
```csharp
notification.Type = NotificationType.Html;
notification.Body = "<b>Important:</b> <em>Action required</em>";
```

[↑ Back to top](#contents)

## Configuration

The notifications channel supports flexible configuration through generic type parameters:

```csharp
public abstract class NotificationsFeederConfiguration : AbstractFeederConfiguration;
```

Custom configurations can extend this base class to add feeder-specific settings.

## Serialization & Contracts

The notification system uses a sophisticated message structure:

- **User Targeting**: `UserId` determines delivery (null = broadcast to all)
- **Deduplication**: `Id` field prevents duplicate notifications
- **State Management**: `Seen` field tracks read/unread status
- **Content Format**: `Type` enum controls text vs HTML rendering
- **Priority Ordering**: `Priority` enum enables filtering and sorting
- **Metadata Extension**: `Metadata` field for custom JSON data

## Performance Notes

- **Snapshot Integration**: Persistent storage for notification history
- **Deduplication**: Automatic prevention of duplicate notifications
- **User Filtering**: Efficient user-specific notification retrieval
- **Broadcast Optimization**: Smart handling of broadcast vs targeted messages
- **Memory Usage**: Snapshot-based storage reduces memory footprint

## RapidStreamer Dependencies

| Package | Version | Description | Links |
|---------|---------|-------------|-------|
| RapidStreamer | 1.0.166-beta.2 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |

## Examples

### Basic Notifications Setup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddNotificationsChannel<MyNotificationConfiguration>(config => 
    {
        config.IsEnabled = true;
    });
}

public class MyNotificationConfiguration : AbstractChannelConfiguration
{
    // Custom configuration properties
}
```

### Sending Targeted Notifications

```csharp
var notification = new NotificationsChannelFeederMessage
{
    UserId = "user123",
    Id = Guid.NewGuid().ToString(),
    Origin = "OrderService",
    Subject = "Order Confirmation",
    Body = "Your order #12345 has been confirmed",
    Priority = NotificationPriority.Normal,
    Type = NotificationType.Text,
    Icon = "order-success"
};

await channel.EmitMessageAsync(notification);
```

### Broadcast Notifications

```csharp
var broadcast = new NotificationsChannelFeederMessage
{
    UserId = null, // Broadcast to all users
    Id = Guid.NewGuid().ToString(),
    Origin = "SystemMaintenance",
    Subject = "Scheduled Maintenance",
    Body = "System will be down for maintenance from 2:00 AM to 4:00 AM",
    Priority = NotificationPriority.High,
    Type = NotificationType.Html,
    Icon = "maintenance"
};

await channel.EmitMessageAsync(broadcast);
```

### HTML Notifications

```csharp
var htmlNotification = new NotificationsChannelFeederMessage
{
    UserId = "user456",
    Subject = "Welcome!",
    Body = "<h2>Welcome to Our Platform!</h2><p>Thank you for joining us. <a href='/getting-started'>Get started here</a>.</p>",
    EllipsisBody = "Welcome to Our Platform! Thank you for joining...",
    Type = NotificationType.Html,
    Priority = NotificationPriority.Normal
};
```

### Custom Feeder Implementation

```csharp
public class EmailNotificationFeeder : IFeeder<NotificationsChannel<MyConfig>, NotificationsChannelFeederMessage>
{
    public async Task SendAsync(NotificationsChannelFeederMessage message)
    {
        // Send email notification
        await emailService.SendAsync(message.UserId, message.Subject, message.Body);
        
        // Emit to channel
        await channel.EmitMessageAsync(message);
    }
}

// Register custom feeder
services.AddNotificationsChannelFeeder<EmailNotificationFeeder, MyConfig, MyFeederConfig>();
```

## See Also

- [../Chat/README.md](../Chat/README.md) — Real-time messaging
- [../../Demo/README.md](../../Demo/README.md) — Demo implementations

[↑ Back to top](#contents)