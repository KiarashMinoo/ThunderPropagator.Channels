# Contributing

Guidance for contributing to this repository. See [`CLAUDE.md`](CLAUDE.md) for the full architecture, unit structure, and conventions this checklist assumes.

## Code review checklist

- [ ] **No channel pipeline may store cross-request state in node-local memory.** State that must survive a request boundary and be visible to other cluster nodes belongs in a shared backing store (a database-backed repository, or the channel's own `SnapshotEntry` store) — never a field on the channel or a pipeline that only this process can see. See issue #46 for the incident this rule comes from: `ChatChannel` held logged-in sessions in a plain in-memory dictionary, so a request landing on a different node than the one a connection logged in on found nothing there.
- [ ] Every new unit in the production area has its five mandatory files (entry point, configuration, feeder message, metadata, DI extensions) and follows the Release-seals/Debug-doesn't pattern.
- [ ] New persistence goes through the multi-provider `DbContext` pattern (all providers updated together — InMemory, EntityFrameworkCore, MongoDB — plus a scaffolded migration for any EF Core-backed test project), not a single provider or an ad hoc store.
- [ ] `dotnet build` and the full test suite (including architecture tests) pass before requesting review.
