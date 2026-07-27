# ThunderPropagator.Channels Documentation

Production-ready real-time channels and demos for ThunderPropagator, including chat, monitoring, notifications, business feeds, and multiplayer games.

## Contents

- [Documentation areas](#documentation-areas)
- [Package dependencies](#package-dependencies)
- [Coverage audit](#coverage-audit)

## Documentation areas

- [Channels](./Channels/README.md) `Types:0` `Files:0` `Diagrams:✓`
- [Demo](./Demo/README.md) `Types:0` `Files:0` `Diagrams:✓`
- [Games](./Games/README.md) `Types:0` `Files:0` `Diagrams:✓`

## Package dependencies

| Package | Version | Registry |
|---|---|---|
| `Bogus` | `35.6.5` | [Package](https://www.nuget.org/packages/Bogus) |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | `10.*` | [Package](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis) |
| `Microsoft.Extensions.Diagnostics.Testing` | `10.*` | [Package](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.Testing) |
| `Microsoft.Extensions.Http.Polly` | `10.*` | [Package](https://www.nuget.org/packages/Microsoft.Extensions.Http.Polly) |
| `NodaTime` | `3.3.2` | [Package](https://www.nuget.org/packages/NodaTime) |

## Coverage audit

| Documentation area | Status | Files | Types | Retry passes |
|---|---|---:|---:|---:|
| [`Channels`](./Channels/README.md) | ✅ Complete | 0 | 0 | 1 |
| [`Channels/Chat`](./Channels/Chat/README.md) | ✅ Complete | 7 | 1 | 1 |
| [`Channels/Chat/Models`](./Channels/Chat/Models/README.md) | ✅ Complete | 1 | 1 | 1 |
| [`Channels/Chat/Models/Groups`](./Channels/Chat/Models/Groups/README.md) | ✅ Complete | 4 | 4 | 1 |
| [`Channels/Chat/Models/Messages`](./Channels/Chat/Models/Messages/README.md) | ✅ Complete | 2 | 2 | 1 |
| [`Channels/Chat/Models/Users`](./Channels/Chat/Models/Users/README.md) | ✅ Complete | 3 | 3 | 1 |
| [`Channels/Chat/Pipelines`](./Channels/Chat/Pipelines/README.md) | ✅ Complete | 0 | 0 | 1 |
| [`Channels/Chat/Pipelines/Groups`](./Channels/Chat/Pipelines/Groups/README.md) | ✅ Complete | 0 | 0 | 1 |
| [`Channels/Chat/Pipelines/Groups/AddUser`](./Channels/Chat/Pipelines/Groups/AddUser/README.md) | ✅ Complete | 2 | 2 | 1 |
| [`Channels/Chat/Pipelines/Groups/Create`](./Channels/Chat/Pipelines/Groups/Create/README.md) | ✅ Complete | 3 | 3 | 1 |
| [`Channels/Chat/Pipelines/Groups/GetAll`](./Channels/Chat/Pipelines/Groups/GetAll/README.md) | ✅ Complete | 2 | 2 | 1 |
| [`Channels/Chat/Pipelines/Groups/Join`](./Channels/Chat/Pipelines/Groups/Join/README.md) | ✅ Complete | 2 | 2 | 1 |
| [`Channels/Chat/Pipelines/Groups/RemoveUser`](./Channels/Chat/Pipelines/Groups/RemoveUser/README.md) | ✅ Complete | 2 | 2 | 1 |
| [`Channels/Chat/Pipelines/Groups/Rename`](./Channels/Chat/Pipelines/Groups/Rename/README.md) | ✅ Complete | 3 | 3 | 1 |
| [`Channels/Chat/Pipelines/Groups/SetIcon`](./Channels/Chat/Pipelines/Groups/SetIcon/README.md) | ✅ Complete | 3 | 3 | 1 |
| [`Channels/Chat/Pipelines/Groups/UserLeave`](./Channels/Chat/Pipelines/Groups/UserLeave/README.md) | ✅ Complete | 2 | 2 | 1 |
| [`Channels/Chat/Pipelines/Messages`](./Channels/Chat/Pipelines/Messages/README.md) | ✅ Complete | 0 | 0 | 1 |
| [`Channels/Chat/Pipelines/Messages/Send`](./Channels/Chat/Pipelines/Messages/Send/README.md) | ✅ Complete | 2 | 2 | 1 |
| [`Channels/Chat/Pipelines/Users`](./Channels/Chat/Pipelines/Users/README.md) | ✅ Complete | 0 | 0 | 1 |
| [`Channels/Chat/Pipelines/Users/Login`](./Channels/Chat/Pipelines/Users/Login/README.md) | ✅ Complete | 4 | 4 | 1 |
| [`Channels/Chat/Pipelines/Users/Register`](./Channels/Chat/Pipelines/Users/Register/README.md) | ✅ Complete | 3 | 3 | 1 |
| [`Channels/Chat/Pipelines/Users/SetAvatar`](./Channels/Chat/Pipelines/Users/SetAvatar/README.md) | ✅ Complete | 2 | 2 | 1 |
| [`Channels/Chat/Pipelines/Users/SetName`](./Channels/Chat/Pipelines/Users/SetName/README.md) | ✅ Complete | 2 | 2 | 1 |
| [`Channels/Chat/Pipelines/Users/Update`](./Channels/Chat/Pipelines/Users/Update/README.md) | ✅ Complete | 3 | 3 | 1 |
| [`Channels/Clock`](./Channels/Clock/README.md) | ✅ Complete | 11 | 1 | 1 |
| [`Channels/NetworkMonitoring`](./Channels/NetworkMonitoring/README.md) | ✅ Complete | 9 | 1 | 1 |
| [`Channels/Notifications`](./Channels/Notifications/README.md) | ✅ Complete | 9 | 4 | 1 |
| [`Channels/ResourceMonitoring`](./Channels/ResourceMonitoring/README.md) | ✅ Complete | 9 | 1 | 1 |
| [`Channels/Throughput`](./Channels/Throughput/README.md) | ✅ Complete | 9 | 1 | 1 |
| [`Channels/TimeZones`](./Channels/TimeZones/README.md) | ✅ Complete | 9 | 1 | 1 |
| [`Channels/TimeZones/WeatherApi`](./Channels/TimeZones/WeatherApi/README.md) | ✅ Complete | 2 | 1 | 1 |
| [`Channels/TimeZones/WeatherApi/Models`](./Channels/TimeZones/WeatherApi/Models/README.md) | ✅ Complete | 4 | 2 | 1 |
| [`Demo`](./Demo/README.md) | ✅ Complete | 0 | 0 | 1 |
| [`Demo/Demo.Airport`](./Demo/Demo.Airport/README.md) | ✅ Complete | 10 | 2 | 1 |
| [`Demo/Demo.Portfolio`](./Demo/Demo.Portfolio/README.md) | ✅ Complete | 7 | 1 | 1 |
| [`Demo/Demo.Portfolio/Pipelines`](./Demo/Demo.Portfolio/Pipelines/README.md) | ✅ Complete | 2 | 2 | 1 |
| [`Demo/Demo.Portfolio/Pipelines/Dtos`](./Demo/Demo.Portfolio/Pipelines/Dtos/README.md) | ✅ Complete | 2 | 2 | 1 |
| [`Demo/Demo.StockListBasic`](./Demo/Demo.StockListBasic/README.md) | ✅ Complete | 9 | 1 | 1 |
| [`Games`](./Games/README.md) | ✅ Complete | 0 | 0 | 1 |
| [`Games/Games.RockPaperScissors`](./Games/Games.RockPaperScissors/README.md) | ✅ Complete | 12 | 3 | 1 |
| [`Games/Games.TicTacToe`](./Games/Games.TicTacToe/README.md) | ✅ Complete | 8 | 1 | 1 |
| [`Games/Games.TicTacToe/Game`](./Games/Games.TicTacToe/Game/README.md) | ✅ Complete | 2 | 2 | 1 |
| [`Games/Games.TicTacToe/Game/Enums`](./Games/Games.TicTacToe/Game/Enums/README.md) | ✅ Complete | 3 | 3 | 1 |
| [`Games/Games.TicTacToe/Game/Exceptions`](./Games/Games.TicTacToe/Game/Exceptions/README.md) | ✅ Complete | 1 | 1 | 1 |
| [`Games/Games.TicTacToe/Game/Players`](./Games/Games.TicTacToe/Game/Players/README.md) | ✅ Complete | 3 | 3 | 1 |
| [`Games/Games.TicTacToe/Pipelines`](./Games/Games.TicTacToe/Pipelines/README.md) | ✅ Complete | 0 | 0 | 1 |
| [`Games/Games.TicTacToe/Pipelines/AddGame`](./Games/Games.TicTacToe/Pipelines/AddGame/README.md) | ✅ Complete | 3 | 3 | 1 |
| [`Games/Games.TicTacToe/Pipelines/GetGames`](./Games/Games.TicTacToe/Pipelines/GetGames/README.md) | ✅ Complete | 2 | 3 | 1 |
| [`Games/Games.TicTacToe/Pipelines/Move`](./Games/Games.TicTacToe/Pipelines/Move/README.md) | ✅ Complete | 3 | 3 | 1 |
| [`Games/Games.TicTacToe/Pipelines/StartGame`](./Games/Games.TicTacToe/Pipelines/StartGame/README.md) | ✅ Complete | 3 | 3 | 1 |

**Last generated:** July 27, 2026
