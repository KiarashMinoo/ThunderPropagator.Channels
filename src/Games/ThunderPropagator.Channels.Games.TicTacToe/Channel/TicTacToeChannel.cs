using System.Collections.Concurrent;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Games.TicTacToe.Game;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Players;
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
        private readonly ConcurrentDictionary<string, TicTacToeGame> _games = [];

        public TicTacToeChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
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

        internal Subscription AddGame(
            IConnectionInfo connectionInfo,
            string requestId,
            string sessionId,
            string playerName,
            PlayerSign sign,
            PlayerKind opponentKind,
            DifficultyLevel difficultyLevel = DifficultyLevel.Easy)
        {
            var player = new HumanPlayer(playerName, sign, connectionInfo.ConnectionId);
            var game = new TicTacToeGame(sessionId, player);
            game.BoardChanged += GameOnBoardChanged;
            game.GameEnded += GameOnGameEnded;

            var subscription = Subscribe(connectionInfo, requestId, sessionId, playerName);

            if (opponentKind == PlayerKind.Computer)
            {
                var computer = new ComputerPlayer(sign == PlayerSign.O ? PlayerSign.X : PlayerSign.O, difficultyLevel);
                game.StartGame(computer);
            }
            else
                _games.TryAdd(sessionId, game);

            return subscription;
        }

        internal IEnumerable<(string SessionId, string PlayerName)> GetGames()
            => _games.Select(game => (game.Key, game.Value.Player1.Name));

        internal Subscription StartGame(
            IConnectionInfo connectionInfo,
            string requestId,
            string sessionId,
            string playerName)
        {
            if (_games.TryGetValue(sessionId, out var game))
            {
                var subscription = Subscribe(connectionInfo, requestId, sessionId, playerName);
                var player = new HumanPlayer(playerName, game.Player1.Sign == PlayerSign.X ? PlayerSign.O : PlayerSign.X, connectionInfo.ConnectionId);
                game.StartGame(player);
                return subscription;
            }

            throw new KeyNotFoundException($"Game {sessionId} not found");
        }

        internal void Move(string sessionId, IConnectionInfo connectionInfo, int row, int column)
        {
            if (_games.TryGetValue(sessionId, out var game))
            {
                if (game.Player1.ConnectionId == connectionInfo.ConnectionId && game.Player1 is HumanPlayer player1)
                {
                    player1.HumanMove(row, column);
                    return;
                }

                // Player2 is null until a second player joins via StartGame, so this must be a
                // null-conditional access — a stray Move call against a not-yet-started game would
                // otherwise throw NullReferenceException instead of falling through to the
                // KeyNotFoundException below.
                if (game.Player2?.ConnectionId == connectionInfo.ConnectionId && game.Player2 is HumanPlayer player2)
                {
                    player2.HumanMove(row, column);
                    return;
                }
            }

            // Issue #37: this used to sit unconditionally after the lookup, so even a successful move
            // above still threw immediately afterward. Also no longer echoes the caller-supplied
            // sessionId, since once the throw only fires on an actual failure, its presence/absence
            // becomes a real session-enumeration signal.
            throw new KeyNotFoundException("Game not found");
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

        private void GameOnGameEnded(object? sender, EventArgs e)
        {
            var game = (TicTacToeGame)sender!;
            _games.TryRemove(game.SessionId, out _);
        }
    }
}
