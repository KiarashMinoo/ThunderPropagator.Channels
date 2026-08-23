# ThunderPropagator.Channels.Demo.Quiz

A multiplayer trivia demo channel built on ThunderPropagator's channel/feeder/pipeline framework. It
ships a fully working, self-contained quiz game (lobby, timed questions, scoring, reveal, and a final
scoreboard) over three WebSocket request keys, and it doubles as a template for building your own
real-time, session-based game channel.

## Registration

```csharp
services.AddQuizChannel(configuration =>
{
    configuration.MaxPlayers = 8;          // default: 8
    configuration.MinPlayers = 2;          // default: 2 — must not exceed MaxPlayers
    configuration.AllowMidGameJoin = true; // default: true

    configuration.FeederConfiguration.LobbyDuration = TimeSpan.FromSeconds(10);
    configuration.FeederConfiguration.QuestionDuration = TimeSpan.FromSeconds(15);
    configuration.FeederConfiguration.RevealingDuration = TimeSpan.FromSeconds(3);
    configuration.FeederConfiguration.ScoreboardDuration = TimeSpan.FromSeconds(5);
    configuration.FeederConfiguration.QuestionsPerGame = int.MaxValue; // default: play the whole bank
});
```

`AddQuizChannel` is idempotent: a second call on the same `IServiceCollection` is a no-op, so it is
safe to call from multiple module-registration paths without guarding it yourself. Invalid
configuration (e.g. `MinPlayers` greater than `MaxPlayers`, or a non-positive duration/count) throws
immediately at startup rather than surfacing later at runtime.

### Configuration reference

| Property | Default | Notes |
|---|---|---|
| `MaxPlayers` | `8` | Must be strictly positive. A genuinely new player is rejected once this many players are connected; a reconnect or existing name never is. |
| `MinPlayers` | `2` | Must be strictly positive and no greater than `MaxPlayers`. Required to be connected before the host can start early. |
| `AllowMidGameJoin` | `true` | When `false`, joining a game that has already left `Lobby` is rejected instead of admitted with a snapshot. |
| `FeederConfiguration.LobbyDuration` | `10s` | How long the demo loop waits in `Lobby` before starting on its own. |
| `FeederConfiguration.QuestionDuration` | `15s` | How long a question stays open for answers. |
| `FeederConfiguration.RevealingDuration` | `3s` | How long the correct answer is shown. |
| `FeederConfiguration.ScoreboardDuration` | `5s` | How long standings are shown before the next question or game end. |
| `FeederConfiguration.QuestionsPerGame` | `int.MaxValue` | Questions played per game, taken from the front of the bank's shuffled order. A value larger than the bank plays the whole bank rather than erroring. |

## WebSocket protocol

All three requests resolve player/host/game identity from server-side connection and session state —
never from a value the request itself supplies — so a connection can only ever act as whichever
player it actually joined as.

### `Quiz/Join`

Joins (or reconnects to) a game's lobby.

| Field | Type | Description |
|---|---|---|
| `GameId` | `string` | The session to join. Must already exist. |
| `PlayerName` | `string` | Display name to join under; whitespace is trimmed/collapsed before use. |

Response: `Subscription`, `IsReconnect` (`bool`), `IsHost` (`bool`), `PlayerName` (`string`, as
normalized). The subscribing connection receives the game's current state as a single unicast
snapshot immediately afterward — see [Snapshot behavior](#snapshot-behavior).

### `Quiz/Answer`

Submits an answer to the currently open question.

| Field | Type | Description |
|---|---|---|
| `GameId` | `string` | The game this answer targets. |
| `QuestionIndex` | `int` | The question this answer targets — rejected as stale if the game has since moved on. |
| `OptionIndex` | `int` | 0-based index into the question's options, never the option text itself. |

Response: `Outcome`, one of `Correct`, `Incorrect`, `WindowClosed` (no question is currently open),
`Duplicate` (this player already answered this question), `Stale` (the index doesn't match the
question that's actually open), or `Invalid` (the option index doesn't exist). The acknowledgement
never reveals the correct answer or what anyone else submitted.

### `Quiz/Start`

Lets the host skip the rest of the lobby wait and start the game immediately.

| Field | Type | Description |
|---|---|---|
| `GameId` | `string` | The game to start. Carries no player identity — the caller's host status is resolved server-side. |

Response: `Outcome`, one of `Started` or `AlreadyStarted` (the game had already left `Lobby`, whether
from an earlier call or a concurrent one that won the race — never an error).

## Game flow and phases

```
Lobby → Question → Revealing → Scoreboard → (next Question, or GameOver)
```

- **Lobby** — players join and wait; the demo loop starts automatically after `LobbyDuration`, or the
  host can skip ahead with `Quiz/Start` once `MinPlayers` are connected.
- **Question** — one question is live; `Quiz/Answer` is accepted until `QuestionDuration` elapses.
- **Revealing** — the correct answer is shown; no further answers are accepted.
- **Scoreboard** — current standings are shown before the next question or the end of the game.
- **GameOver** — final standings and the winner are shown; only a restart leaves this phase.

Every subscriber receives the new state on each phase transition. Two fields are redacted from the
wire message outside the phase that's supposed to reveal them, regardless of when they were actually
computed internally: `CorrectAnswer` reads empty before `Revealing`, and `Winner` reads empty before
`GameOver`.

## Cast

- **Host** — the player whose `Quiz/Join` call created the session. Fixed for the session's lifetime
  (a disconnected host is still the host until they reconnect); only the host may call `Quiz/Start`.
- **Player** — any other joined participant. A live duplicate of an already-connected player name is
  rejected; a name that belongs to a disconnected player instead reconnects that same identity under
  the new connection.

## Snapshot behavior

`GameId` is the game's sole subscribing key — a client subscribes to one session and receives every
field of it, rather than subscribing per-field. A newly (re)joining connection is handed the game's
current state as a single unicast snapshot through its own subscription, using the channel's ordinary
snapshot-replay-on-subscribe mechanism; nothing re-emits it manually, since doing so would risk
delivering it twice. Subsequent phase transitions and updates then arrive as broadcasts to every
subscriber of that `GameId`.

## Security and host assumptions

- Every identity-sensitive action (`Quiz/Answer`, `Quiz/Start`) resolves the acting player, and the
  host check, from server-side connection/session state — a request can never claim to be a different
  player or the host by putting a different identity in its payload.
- `Quiz/Start` additionally requires at least `MinPlayers` connected players; below that it throws
  rather than starting a game nobody can meaningfully play.
- This package only establishes per-game membership and host status on top of whatever connection
  identity the host application's own transport already provides — it performs no authentication of
  its own, and is intended as a demo/reference implementation rather than a production authorization
  layer.

## Built-in simulation vs. a production provider

This package ships two independent ways to drive a game, and they are designed to coexist:

- **Built-in demo simulation** — registered automatically by `AddQuizChannel`, this runs one
  perpetual game under the fixed `GameId` `"demo"`, cycling through the phases above using a built-in
  question bank, and only advancing while at least one subscriber is present.
- **Production provider** — call `services.AddChannelProvider()` in addition to `AddQuizChannel` to
  resolve `QuizChannel` as `IProvider<QuizProviderMessage>`. A host application can then call
  `PublishAsync` to push its own externally-produced quiz state (its own questions, scoring, and
  timing) through the same channel, entirely independent of the built-in simulation's session and
  membership state.

The two coexist safely **only for different `GameId` values** — the built-in simulation always drives
the literal id `"demo"`, so a provider-driven host must never reuse that value for its own sessions.
There is currently no configuration switch to disable the built-in simulation, so a deployment that
wants provider-only behavior for a shared `GameId` cannot do so through `AddQuizChannel` alone.

## Errors

| Exception | Thrown when |
|---|---|
| `QuizChannelConfigurationValidationException` | `AddQuizChannel` configuration is invalid (e.g. `MinPlayers` > `MaxPlayers`, or a non-positive value). |
| `QuizGameNotFoundException` | The `GameId` on a `Join`/`Answer`/`Start` request doesn't correspond to an existing session. |
| `QuizGameFullException` | A genuinely new player tries to join once `MaxPlayers` are already connected. |
| `QuizNonLobbyJoinNotAllowedException` | `AllowMidGameJoin` is `false` and the game has already left `Lobby`. |
| `QuizInvalidPlayerNameException` | The normalized player name exceeds the maximum length. |
| `QuizNotAJoinedPlayerException` | `Answer`/`Start` is called from a connection that never joined the game. |
| `QuizNotTheHostException` | `Start` is called by a joined player who is not the host. |
| `QuizNotEnoughPlayersException` | `Start` is called with fewer than `MinPlayers` connected. |
| `QuizProviderValidationException` | A provider-published message lacks required question content for its phase. |

## See also

- [`ThunderPropagator.Channels.Demo.Quiz.UnitTests`](../../../Tests/Demo/ThunderPropagator.Channels.Demo.Quiz.UnitTests) and
  [`Demo/Quiz` under `ThunderPropagator.UnitTests`](../../../Tests/UnitTests/ThunderPropagator.UnitTests/Demo/Quiz) for
  runnable examples of every flow described above, including reconnect, concurrent starts, and the
  provider-driven path.
- [Repository root README](../../../README.md) for build/restore instructions shared by every package
  in this repository.
