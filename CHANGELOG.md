# Changelog

All notable changes to this project will be documented in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.0.1-beta.17] — 2026-08-18

### 🚀 Features

- Add XML documentation to the public notifications API `(48d96df)` — Kiarash Minoo

## [1.0.1-beta.16] — 2026-08-18

### 🚀 Features

- Use idiomatic default cancellation tokens in SnapshotsToSendAsync `(56478ac)` — Kiarash Minoo

## [1.0.1-beta.15] — 2026-08-18

### 🚀 Features

- Capture Date and Time once when a message is constructed `(8b69dca)` — Kiarash Minoo

## [1.0.1-beta.14] — 2026-08-18

### 🚀 Features

- Derive EllipsisBody consistently from Body `(1a09630)` — Kiarash Minoo

## [1.0.1-beta.13] — 2026-08-18

### 🚀 Features

- Add batching, deduplication, TTL, and retry options to feeder configuration `(fe0c5d3)` — Kiarash Minoo

## [1.0.1-beta.12] — 2026-08-18

### 🚀 Features

- Remove Date from subscription keys and support optional historical date filtering `(77908b6)` — Kiarash Minoo

## [1.0.1-beta.11] — 2026-08-18

### 🚀 Features

- Allow message construction through immutable init accessors `(95d6452)` — Kiarash Minoo

## [1.0.1-beta.10] — 2026-08-18

### 🚀 Features

- Create an isolated message instance for every broadcast recipient `(bbb70cb)` — Kiarash Minoo

## [1.0.1-beta.9] — 2026-08-18

### 🚀 Features

- Add a copy constructor for NotificationsChannelFeederMessage `(2b062fb)` — Kiarash Minoo

## [1.0.1-beta.8] — 2026-08-18

### 🚀 Features

- Remove sync-over-async calls from Notifications subscription/emission `(88ddfee)` — Kiarash Minoo

## [1.0.1-beta.7] — 2026-08-18

### 🧪 Tests

- Add channel-level integration coverage for IterativeFeeder shutdown `(ea081d2)` — Kiarash Minoo

## [1.0.1-beta.6] — 2026-08-18

### 🚀 Features

- Prevent busy-spin loops in demo feeders via configurable poll intervals `(0e0a395)` — Kiarash Minoo
- Route new poll-interval properties through Get/Set instead of plain auto-properties `(fd43c6d)` — Kiarash Minoo

## [1.0.1-beta.5] — 2026-08-18

### 🚀 Features

- Bump JetBrains.Annotations from 2025.2.4 to 2026.2.0 `(b04a085)` — dependabot[bot]
- Fail fast when UtilizationWindow conversion overflows `(f9c788c)` — Kiarash Minoo
- Document and stress-test MetricCollector thread-safety `(6cfcc62)` — Kiarash Minoo
- Skip polling output work when there are no subscriptions `(d654697)` — Kiarash Minoo

### ♻️ Refactoring

- streamline message handling and update package versions `(51cdf11)` — Kiarash Minoo

### 📦 Dependencies

| Package | Old | New |
|---------|-----|-----|
| NodaTime | 3.3.2 | 3.3.3 |
| Microsoft.NET.Test.Sdk | 18.5.1 | 18.8.1 |
| NSubstitute | 5.3.0 | 6.0.0 |
| coverlet.collector | 10.0.0 | 10.0.1 |
| NSubstitute | 6.0.0 | 6.1.0 |
| Microsoft.NET.Test.Sdk | 18.8.1 | 18.9.0 |
| NSubstitute | 6.1.0 | 6.2.0 |
| xunit.runner.visualstudio | 3.1.5 | 4.0.0 |

- Bump NodaTime from 3.3.2 to 3.3.3 `(c0871c5)` — dependabot[bot]
- Bump the testing group with 3 updates `(8feecb8)` — dependabot[bot]
- Bump the testing group with 1 update `(8eb399b)` — dependabot[bot]
- Bump the testing group with 3 updates `(a1fca4b)` — dependabot[bot]

### ⚙️ CI / Tooling

- Add security workflows and NuGet publish jobs `(401c09b)` — Kiarash Minoo
- Adjust dependency and security workflows `(c96411a)` — Kiarash Minoo
- Streamline CI workflows and concurrency `(18379ac)` — Kiarash Minoo
- enable nuget-filter-enabled to stop publishing every platform/config package variant `(06eb001)` — Kiarash Minoo

### 📝 Documentation

- rebuild repository documentation `(180a311)` — Codex
- update CLAUDE.md with streamlined guidance and architecture rules `(928d8cf)` — Kiarash Minoo

### 🧪 Tests

- Add regression coverage for cancellation during a pending feeder delay `(4f1eeb4)` — Kiarash Minoo

### 🏠 Chores

- Ignore TFM-pinned packages in Dependabot `(fdfd317)` — Kiarash Minoo
- Bump version to 1.0.1-beta.4 `(441e9a5)` — Kiarash Minoo

## [Unreleased]

### 🚀 Features

- Implement TicTacToe channel with add, get, move, and start game functionalities `(d884579)`
- Implement Chat channel with user, group, and message support `(30ede1e)`
- Add additional Chat pipelines for groups and messages `(5b78188)`
- Add additional user pipelines for Chat channel `(f92b7a2)`
- Add system resource monitoring channel `(d3ad989)`
- Add demo channels (Airport, Portfolio, StockListBasic) and game channels (RockPaperScissors) `(5846cfd)`
- Add feeder configurations as nested properties on channel configurations `(1572ec0)`
- Make TimeZones channel configurable `(ea7169c)`
- Add ARM64 platform configuration to solution `(70971cc)`
- Add .NET 9 multi-targeting support with framework-specific package references `(d22581f)`
- Add `ConnectionStringHelper.EnrichConnectionString` for connection strings `(9a0f379)`

### 🐛 Bug Fixes

- Fix Portfolio demo channel problem `(512653c)`
- Fix TicTacToe channel pipelines `(74ff8e6)`
- Set default values for feeder message fields `(2c1b775)`
- Fix feeder message property bugs `(e73316c)`
- Add missing dependency injection registrations `(2679c66)`
- Fix Version property resolution in project files `(98396db)`
- Fix solution build configurations `(0027fda)`
- Fix package builder targeting on x86 and x64 platforms `(0a8c8fe)`
- Fix RepositoryUrl in project properties `(590e2e7)`

### ♻️ Refactoring

- Refactor project files and dependencies across multiple channels `(c696550)`
- Replace PackageVersion with Version in project files for central package management `(54bbe13)`

### 🧪 Tests

- Add ArchTests project for architecture validation `(34fb41b)`
- Add unit tests for models and enums across all channels `(e0a4f50)`
- Update tests to assert internal visibility for feeder messages and pipelines `(d1ac98a)`
- Add `InternalsVisibleTo` attribute to AssemblyInfo.cs files for unit test access `(2578e85)`

### ⚙️ CI / Tooling

- Add CI workflows for beta and release processes including version bumping, packing, and publishing `(4c78d87)`
- Add GitHub Actions workflow for cleaning up old GitHub Packages `(670bc1f)`
- Simplify package references in ArchTests project file `(2ac7727)`
- Mark unit test projects as non-packable `(114449e)`

### 📝 Documentation

- Add comprehensive documentation with Mermaid diagrams for all channels `(fa42124)`
- Update README.md `(5e9b332)`

### 📦 Dependencies

| Package | Old | New |
|---------|-----|-----|
| RapidStreamer (renamed to ThunderPropagator) | 1.0.1-beta.3 | ThunderPropagator 1.0.1-beta.15 |

Recurring NuGet dependency upgrades across the project lifecycle `(803244a)` `(bf63625)` `(fe31358)` `(9d97d33)` `(f61e466)` `(2068e7b)` `(73b5323)` `(059209s)` `(839c083)` `(b53cddf)` `(5f30767)` `(c338856)` `(ebc4ec1)` `(d2fcc96)` `(ebb48ac)` `(5dae1ad)` `(d08cca4)` `(ca288ec)` `(0bf2a99)` `(5eb8771)` `(82daaf3)` `(9edb29c)` `(c8896c0)` `(4b6e722)` `(8dbc70d)` `(e3ee936)` `(8e7e9d5)` `(de7797d)` `(9375ea9)` `(7f3e0e0)` `(553774c)` `(49f6275)` `(1a23d15)` `(c819720)` `(a1f643e)` `(6940f10)` `(83716fd)` `(f14ec85)` `(9d97d33)` `(f497790)` `(83b8f58)` `(4bbed35)` `(c2255a4)`

