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
