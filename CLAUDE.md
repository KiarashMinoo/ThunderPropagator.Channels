# CLAUDE.md

<!-- Humans: keep this file lean — production-unit implementation patterns live in .claude/rules/, not here. -->

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

First restore against the private feed needs a read token as an env var, added as a NuGet source.

## Architecture

Three sibling top-level areas, isolated (architecture-test enforced), none may depend on the others: production, demo/sample, games/playground.

Production-unit file structure and code patterns: `.claude/rules/production-units.md`.

## Architecture Rules (Enforced)

- The three top-level areas must not cross-depend.
- Entry-point-suffix types: abstract or sealed.
- Configuration/feeder/pipeline/metadata/feeder-message/request-DTO/response-DTO-suffix types: public.
- Extensions-suffix types: static and public.
- Exception-suffix types: inherit the base exception type.

## Conventions

Nullable + implicit usings on; private fields `_camelCase`; telemetry activities `{ClassName}_{MethodName}`; platform names mixed inner-case, not all-caps; XML docs on public API; preview language features in test projects only.

## Build & Versioning

Version/TFMs centralized; CI bumps automatically — never hand-edit outside a release workflow. Package id carries a debug/platform suffix by configuration.
