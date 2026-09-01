using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Games.TicTacToe.Game;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Players;
using ThunderPropagator.Channels.Games.TicTacToe.Models;
using ThunderPropagator.Infrastructure.Receivers.Pipelines.Subscribe;
using ThunderPropagator.Channels.Games.TicTacToe.Configuration;
using ThunderPropagator.Channels.Games.TicTacToe.Messages;
using ThunderPropagator.Channels.Games.TicTacToe.Metadata;

namespace ThunderPropagator.Channels.Games.TicTacToe.Channel
{
    [Unsubscribable]
    public
#if !DEBUG
        sealed
#endif
        class TicTacToeChannel : AbstractChannel<TicTacToeChannelMetadata, TicTacToeChannelConfiguration>
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // Issue: TicTacToeGameService is Scoped (like RockPaperScissors's own
        // RockPaperScissorsMatchmakingService — see #288/#46), so it can't be injected directly into
        // this Singleton channel — a fresh scope is created per call instead.
        public TicTacToeChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        }

        private Task<T> UseGameServiceAsync<T>(Func<TicTacToeGameService, Task<T>> action)
        {
            using var scope = _scopeFactory.CreateScope();
            return action(scope.ServiceProvider.GetRequiredService<TicTacToeGameService>());
        }

        private Subscription Subscribe(IConnectionInfo connectionInfo, string requestId, string sessionId, string playerName)
        {
            var subscribeRequest = new TicTacToeChannelSubscribeRequest
            {
                SubscribingKeys = new Dictionary<string, IReadOnlyDictionary<string, string>>
                {
                    {
                        nameof(TicTacToeChannelFeederMessage.SessionId), new Dictionary<string, string>
                        {
                            { nameof(TicTacToeChannelFeederMessage.SessionId), sessionId },
                            { nameof(TicTacToeChannelFeederMessage.PlayerName), playerName },
                        }
                    }
                },
                SubscribingFields = new HashSet<string>
                {
                    nameof(TicTacToeChannelFeederMessage.Row),
                    nameof(TicTacToeChannelFeederMessage.Column),
                    nameof(TicTacToeChannelFeederMessage.Sign),
                },
                SubscriptionMode = SubscriptionMode.Full
            };

            return Subscribe(connectionInfo, requestId, subscribeRequest).Single();
        }

        internal Task<Subscription> AddGameAsync(
            IConnectionInfo connectionInfo,
            string requestId,
            string sessionId,
            string playerName,
            PlayerSign sign,
            PlayerKind opponentKind,
            DifficultyLevel difficultyLevel = DifficultyLevel.Easy,
            CancellationToken cancellationToken = default)
            => UseGameServiceAsync(async gameService =>
            {
                var player1 = new HumanPlayer(playerName, sign, connectionInfo.ConnectionId);
                var game = new TicTacToeGame(sessionId, player1);

                var subscription = Subscribe(connectionInfo, requestId, sessionId, playerName);

                var record = TicTacToeGameRecord.CreateWaitingForOpponent(sessionId, SerializeBoard(game), playerName, sign, connectionInfo.ConnectionId);

                // Issue: a vs-Computer game used to never be persisted at all here — every subsequent
                // Move against it threw "Game not found," so the computer opponent was entirely
                // unplayable. StartGame is still called immediately (the computer never needs a
                // separate "join" step), but the resulting state — Player2 present, board, whose turn
                // — is now captured into the record before returning, same as StartGameAsync does for
                // a joining human opponent.
                if (opponentKind == PlayerKind.Computer)
                {
                    var computer = new ComputerPlayer(sign == PlayerSign.O ? PlayerSign.X : PlayerSign.O, difficultyLevel);
                    game.StartGame(computer);
                    record.Start(SerializeBoard(game), computer.Name, PlayerKind.Computer, null, difficultyLevel, game.CurrentTurnSign!.Value);
                }

                await gameService.CreateGameAsync(record, cancellationToken);

                return subscription;
            });

        internal Task<IEnumerable<(string SessionId, string PlayerName)>> GetGamesAsync(CancellationToken cancellationToken = default)
            => UseGameServiceAsync(async gameService =>
            {
                var records = await gameService.GetOpenGamesAsync(cancellationToken);
                return records.Select(record => (record.SessionId, record.Player1Name));
            });

        internal Task<Subscription> StartGameAsync(
            IConnectionInfo connectionInfo,
            string requestId,
            string sessionId,
            string playerName,
            CancellationToken cancellationToken = default)
            => UseGameServiceAsync(async gameService =>
            {
                var record = await gameService.GetGameAsync(sessionId, cancellationToken)
                    ?? throw new KeyNotFoundException($"Game {sessionId} not found");

                // Issue: the original never guarded against a third caller "joining" an
                // already-started game — doing so replaced the real Player2 reference and re-wired
                // its turn-order handlers a second time, corrupting the running game. Same generic
                // not-found message as an unknown sessionId, for the same anti-enumeration reasoning
                // TicTacToeChannelMoveReceiverPipeline's own history already established for Move.
                if (record.Player2Kind is not null)
                    throw new KeyNotFoundException($"Game {sessionId} not found");

                var game = RehydrateWaitingGame(record);
                var subscription = Subscribe(connectionInfo, requestId, sessionId, playerName);
                var player2Sign = game.Player1.Sign == PlayerSign.X ? PlayerSign.O : PlayerSign.X;
                var player2 = new HumanPlayer(playerName, player2Sign, connectionInfo.ConnectionId);
                game.StartGame(player2);

                record.Start(SerializeBoard(game), playerName, PlayerKind.Human, connectionInfo.ConnectionId, null, game.CurrentTurnSign!.Value);
                await gameService.SaveGameAsync(record, cancellationToken);

                return subscription;
            });

        internal async Task MoveAsync(string sessionId, IConnectionInfo connectionInfo, int row, int column, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var gameService = scope.ServiceProvider.GetRequiredService<TicTacToeGameService>();

            var record = await gameService.GetGameAsync(sessionId, cancellationToken);

            // Issue: a move against a game that hasn't started yet (no Player2) used to silently
            // reach the board with no turn-check, no win-check, and no notification to anyone —
            // TicTacToeGame's turn-order handlers are only wired by StartGame, so a lone Player1 could
            // freely place marks before a second player ever joined. Same generic not-found message
            // as an unknown sessionId, both here for the pre-existing anti-enumeration reasoning
            // (issue #37) and to avoid distinguishing "doesn't exist" from "not yours to play yet."
            if (record is null || record.Player2Kind is null)
                throw new KeyNotFoundException("Game not found");

            var game = RehydrateStartedGame(record);
            var ended = false;
            game.GameEnded += (_, _) => ended = true;
            game.BoardChanged += GameOnBoardChanged;

            if (game.Player1.ConnectionId == connectionInfo.ConnectionId && game.Player1 is HumanPlayer player1)
                player1.HumanMove(row, column);
            else if (game.Player2?.ConnectionId == connectionInfo.ConnectionId && game.Player2 is HumanPlayer player2)
                player2.HumanMove(row, column);
            else
                // Issue #37: this used to sit unconditionally after the lookup, so even a successful
                // move above still threw immediately afterward. Also no longer echoes the
                // caller-supplied sessionId, since once the throw only fires on an actual failure, its
                // presence/absence becomes a real session-enumeration signal.
                throw new KeyNotFoundException("Game not found");

            if (ended)
                await gameService.DeleteGameAsync(sessionId, cancellationToken);
            else
            {
                record.ApplyMove(SerializeBoard(game), game.CurrentTurnSign!.Value);
                await gameService.SaveGameAsync(record, cancellationToken);
            }
        }

        private void GameOnBoardChanged(object? sender, BoardChangedEventArgs e)
        {
            var game = (TicTacToeGame)sender!;

            EmitMessage(new TicTacToeChannelFeederMessage
            {
                SessionId = game.SessionId,
                PlayerName = game.Player1.Name,
                Sign = e.Player.Sign,
                Row = e.Row,
                Column = e.Column,
            });

            if (game.Player2 is not ComputerPlayer)
            {
                EmitMessage(new TicTacToeChannelFeederMessage
                {
                    SessionId = game.SessionId,
                    PlayerName = game.Player2.Name,
                    Sign = e.Player.Sign,
                    Row = e.Row,
                    Column = e.Column,
                });
            }
        }

        private static TicTacToeGame RehydrateWaitingGame(TicTacToeGameRecord record)
        {
            var player1 = new HumanPlayer(record.Player1Name, record.Player1Sign, record.Player1ConnectionId);
            return new TicTacToeGame(record.SessionId, player1);
        }

        private static TicTacToeGame RehydrateStartedGame(TicTacToeGameRecord record)
        {
            var game = RehydrateWaitingGame(record);
            var player2Sign = game.Player1.Sign == PlayerSign.X ? PlayerSign.O : PlayerSign.X;

            Player player2 = record.Player2Kind == PlayerKind.Computer
                ? new ComputerPlayer(player2Sign, record.Player2DifficultyLevel!.Value)
                : new HumanPlayer(record.Player2Name!, player2Sign, record.Player2ConnectionId!);

            game.StartGame(player2);
            game.RestoreState(DeserializeBoard(record.Board), record.CurrentTurnSign!.Value);

            return game;
        }

        private static string SerializeBoard(TicTacToeGame game)
        {
            var cells = new char[9];
            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    cells[row * 3 + column] = game.SignAt(row, column) switch
                    {
                        null => '-',
                        PlayerSign.X => 'X',
                        PlayerSign.O => 'O',
                        _ => throw new InvalidOperationException($"Unsupported sign at ({row},{column}).")
                    };
                }
            }

            return new string(cells);
        }

        private static IReadOnlyList<PlayerSign?> DeserializeBoard(string board)
            => board.Select(cell => cell switch
            {
                'X' => (PlayerSign?)PlayerSign.X,
                'O' => PlayerSign.O,
                _ => null
            }).ToList();
    }
}
