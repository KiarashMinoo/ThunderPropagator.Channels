---
paths:
  - "src/Channels/**"
---

# Production-Area Unit Structure

Every unit in the production area has exactly five files (architecture-test enforced):

| Role | Base type | Visibility |
|---|---|---|
| Entry point | generic channel base (metadata, config type params) | public, sealed in Release |
| Configuration | channel-configuration base | public |
| Feeder message | dictionary-backed message base | internal |
| Metadata | channel-metadata base | public |
| DI extensions | static class | public static |

Entry-point classes: `#if !DEBUG sealed #endif`.

## Two Core Patterns

**Feeder** (push-only): inherit the iterative-feeder base (channel/message/config type params), implement receive as `IAsyncEnumerable` yielding received-message wrappers. Internal by convention.

```csharp
internal class {Name}Feeder : IterativeFeeder<{Name}Channel, {Name}ChannelFeederMessage, {Name}FeederConfiguration>
{
    protected override async IAsyncEnumerable<FeederReceivedMessage<{Name}ChannelFeederMessage>> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // await the next value, then yield a message
    }
}
```

**Pipeline** (bidirectional): receive pipeline per concern in a domain-named folder — pipeline class + request DTO + optional response DTO, all public.

## Feeder-Message Properties

```csharp
public string Key
{
    get => GetValueOrDefault(string.Empty);
    private set => SetValue(value);
}
```

## DI Registration

```csharp
services
    .AddSingleton(configuration)
    .AddChannel<{Name}Channel>()
    .AddChannelFeeder<{Name}Channel, {Name}Feeder, {Name}ChannelFeederMessage, {Name}FeederConfiguration>(...);
```
