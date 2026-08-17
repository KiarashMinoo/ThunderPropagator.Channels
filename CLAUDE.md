# CLAUDE.md

Guidance for working in this repository.

## Commands

```powershell
dotnet restore
dotnet build -c Release
dotnet build -c Release -p:Platform=x64   # or ARM64
dotnet test -c Release
dotnet test --filter "FullyQualifiedName~<Name>"
dotnet test --collect:"XPlat Code Coverage"
dotnet pack -c Release -p:Platform=x64
```

First restore against the private package feed needs a read token set as an environment variable and added as a NuGet source.

## Architecture

Three sibling top-level areas, isolated from each other and checked by an architecture-test project: a production area, a demo/sample area, and a games/playground area. None may depend on either of the other two.

## Mandatory unit structure

Every unit in the production area has exactly five files, enforced by architecture tests:

| Role | Base type | Visibility |
|---|---|---|
| Entry point | generic channel base (metadata, configuration type parameters) | public, sealed in Release |
| Configuration | channel-configuration base | public |
| Feeder message | dictionary-backed message base | internal |
| Metadata | channel-metadata base | public |
| DI extensions | static class | public static |

Entry-point classes use the Release-seals/Debug-doesn't pattern (`#if !DEBUG sealed #endif`).

## Two core patterns

**Feeder** (push-only): inherit the iterative-feeder base (channel/message/config type parameters), implement the receive method as an `IAsyncEnumerable` yielding received-message wrappers. Feeders are internal by convention.

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

**Pipeline** (bidirectional): a receive pipeline per concern lives in a domain-named folder, with a pipeline class plus a request DTO and an optional response DTO — all three public.

## Feeder-message properties

```csharp
public string Key
{
    get => GetValueOrDefault(string.Empty);
    private set => SetValue(value);
}
```

## DI registration

```csharp
services
    .AddSingleton(configuration)
    .AddChannel<{Name}Channel>()
    .AddChannelFeeder<{Name}Channel, {Name}Feeder, {Name}ChannelFeederMessage, {Name}FeederConfiguration>(...);
```

## Architecture rules (enforced)

- The three top-level areas must not cross-depend.
- Types ending in the entry-point suffix must be abstract or sealed.
- Types ending in a configuration/feeder/pipeline/metadata/feeder-message/request-DTO/response-DTO suffix must be public.
- Types ending in an extensions suffix must be static and public.
- Types ending in an exception suffix must inherit from the base exception type.

## Conventions

Nullable + implicit usings on; private fields `_camelCase`; telemetry activities named `{ClassName}_{MethodName}`; platform names in mixed inner-case, not all-caps; XML docs required on public API; preview language features only in test projects.

## Build & versioning

Version and target frameworks are centralized; CI bumps automatically — never hand-edit outside a release workflow. Package id carries a debug/platform suffix depending on configuration.
