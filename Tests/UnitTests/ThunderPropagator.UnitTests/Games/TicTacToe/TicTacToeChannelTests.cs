using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Games.TicTacToe.Channel;
using ThunderPropagator.Channels.Games.TicTacToe.Configuration;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;
using ThunderPropagator.Channels.Games.TicTacToe.Metadata;
using ThunderPropagator.Channels.Games.TicTacToe.Models;

namespace ThunderPropagator.UnitTests.Games.TicTacToe
{
    /// <summary>
    /// Issue: TicTacToeChannel's own session state moved from a node-local dictionary to the persisted
    /// TicTacToeGameService (mirrors RockPaperScissors' own #288/#46). Along the way, three bugs that
    /// made the module unplayable were fixed and are covered here:
    /// - AddGameAsync never persisted a vs-Computer game at all, so every Move against a computer
    ///   opponent threw "Game not found" — see AddGameAsync_WithComputerOpponent_ThenMoveAsync_DoesNotThrow.
    /// - The game's "whose turn" state was never initialized, so the first move after StartGame always
    ///   threw InvalidMoveException — see AddGameAsync_StartGameAsync_ThenMoveAsync_ByPlayer1_DoesNotThrow.
    /// - Nothing rejected a move before a second player joined, or a second StartGameAsync call on an
    ///   already-started game — both now throw the same generic "not found" MoveAsync/StartGame's own
    ///   existing anti-enumeration convention (issue #37) already established.
    /// </summary>
    public sealed class TicTacToeChannelTests
    {
        private sealed class FakeTicTacToeContext : ITicTacToeContext
        {
            private readonly Dictionary<string, TicTacToeGameRecord> _games = [];

            public Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => Task.FromResult(_games.TryGetValue((string)(object)id!, out var game) ? (TEntity)(object)game : null);

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
                => Task.FromResult<IReadOnlyCollection<TEntity>>(_games.Values.Cast<TEntity>().ToList());

            public Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
            {
                var game = (TicTacToeGameRecord)(object)entity!;
                _games[game.SessionId] = game;
                return Task.FromResult(entity);
            }

            public Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
            {
                var game = (TicTacToeGameRecord)(object)entity!;
                _games[game.SessionId] = game;
                return Task.FromResult(entity);
            }

            public Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => Task.FromResult(_games.Remove((string)(object)id!));
        }

        private static TicTacToeChannel CreateChannel()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ITicTacToeContext>(new FakeTicTacToeContext());
            services.AddScoped<TicTacToeGameService>();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(new TicTacToeChannelConfiguration());
            services.AddSingleton(Substitute.For<IHostApplicationLifetime>());

            var channel = new TicTacToeChannel(services.BuildServiceProvider());
            channel.Initialize(CancellationToken.None);

            return channel;
        }

        private static IConnectionInfo CreateConnection(string connectionId)
        {
            var connectionInfo = Substitute.For<IConnectionInfo>();
            connectionInfo.ConnectionId.Returns(connectionId);
            return connectionInfo;
        }

        [Fact]
        public void TicTacToeChannel_IsPublic()
        {
            var type = typeof(TicTacToeChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public async Task MoveAsync_ForAnUnknownSession_ThrowsKeyNotFoundExceptionWithoutEchoingTheSessionId()
        {
            var channel = CreateChannel();
            var connectionInfo = CreateConnection("connection-1");

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => channel.MoveAsync("super-secret-session-id", connectionInfo, 0, 0));

            Assert.DoesNotContain("super-secret-session-id", exception.Message);
        }

        [Fact]
        public async Task MoveAsync_BeforeAnyoneCallsStartGame_ThrowsKeyNotFoundException()
        {
            // Issue: a move against a game with no second player used to reach the board with no
            // turn-check, no win-check, and no notification to anyone, since TicTacToeGame's
            // turn-order handlers are only wired by StartGame.
            var channel = CreateChannel();
            var connectionInfo = CreateConnection("connection-1");
            await channel.AddGameAsync(connectionInfo, "request-1", "session-1", "Alice", PlayerSign.X, PlayerKind.Human);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => channel.MoveAsync("session-1", connectionInfo, 0, 0));
        }

        [Fact]
        public async Task AddGameAsync_StartGameAsync_ThenMoveAsync_ByPlayer1_DoesNotThrow()
        {
            // Issue: TicTacToeGame's "whose turn" state was never initialized anywhere, so this exact
            // sequence — the very first move once a second player has joined — always threw
            // InvalidMoveException, for every game, human or computer opponent.
            var channel = CreateChannel();
            var player1Connection = CreateConnection("connection-1");
            var player2Connection = CreateConnection("connection-2");
            await channel.AddGameAsync(player1Connection, "request-1", "session-1", "Alice", PlayerSign.X, PlayerKind.Human);
            await channel.StartGameAsync(player2Connection, "request-2", "session-1", "Bob");

            var exception = await Record.ExceptionAsync(() => channel.MoveAsync("session-1", player1Connection, 0, 0));

            Assert.Null(exception);
        }

        [Fact]
        public async Task MoveAsync_ByAConnectionThatIsNotTheGamesPlayer_ThrowsKeyNotFoundException()
        {
            var channel = CreateChannel();
            var ownerConnection = CreateConnection("owner-connection");
            var opponentConnection = CreateConnection("opponent-connection");
            var intruderConnection = CreateConnection("intruder-connection");
            await channel.AddGameAsync(ownerConnection, "request-1", "session-1", "Alice", PlayerSign.X, PlayerKind.Human);
            await channel.StartGameAsync(opponentConnection, "request-2", "session-1", "Bob");

            await Assert.ThrowsAsync<KeyNotFoundException>(() => channel.MoveAsync("session-1", intruderConnection, 0, 0));
        }

        [Fact]
        public async Task StartGameAsync_ForAnUnknownSession_ThrowsKeyNotFoundException()
        {
            var channel = CreateChannel();
            var connectionInfo = CreateConnection("connection-1");

            await Assert.ThrowsAsync<KeyNotFoundException>(() => channel.StartGameAsync(connectionInfo, "request-1", "unknown-session", "Bob"));
        }

        [Fact]
        public async Task StartGameAsync_OnAnAlreadyStartedGame_ThrowsKeyNotFoundException()
        {
            // Issue: nothing previously stopped a third caller from "joining" an already-started game
            // — doing so replaced the real Player2 reference and re-wired its turn-order handlers a
            // second time, corrupting the running game.
            var channel = CreateChannel();
            var player1Connection = CreateConnection("connection-1");
            var player2Connection = CreateConnection("connection-2");
            var intruderConnection = CreateConnection("intruder-connection");
            await channel.AddGameAsync(player1Connection, "request-1", "session-1", "Alice", PlayerSign.X, PlayerKind.Human);
            await channel.StartGameAsync(player2Connection, "request-2", "session-1", "Bob");

            await Assert.ThrowsAsync<KeyNotFoundException>(() => channel.StartGameAsync(intruderConnection, "request-3", "session-1", "Eve"));
        }

        [Fact]
        public async Task AddGameAsync_WithComputerOpponent_ThenMoveAsync_DoesNotThrow()
        {
            // Issue: AddGameAsync used to never persist (nor, before that, even store in the old
            // in-memory dictionary) a vs-Computer game at all — every Move against it threw "Game not
            // found," making the computer opponent entirely unplayable.
            var channel = CreateChannel();
            var connectionInfo = CreateConnection("connection-1");
            await channel.AddGameAsync(connectionInfo, "request-1", "session-1", "Alice", PlayerSign.X, PlayerKind.Computer, DifficultyLevel.Easy);

            var exception = await Record.ExceptionAsync(() => channel.MoveAsync("session-1", connectionInfo, 0, 0));

            Assert.Null(exception);
        }

        [Fact]
        public async Task GetGamesAsync_OnlyReturnsGamesStillWaitingForASecondPlayer()
        {
            var channel = CreateChannel();
            var waitingConnection = CreateConnection("waiting-connection");
            var startedConnection = CreateConnection("started-connection");
            var opponentConnection = CreateConnection("opponent-connection");
            var computerConnection = CreateConnection("computer-connection");

            await channel.AddGameAsync(waitingConnection, "request-1", "waiting-session", "Alice", PlayerSign.X, PlayerKind.Human);
            await channel.AddGameAsync(startedConnection, "request-2", "started-session", "Bob", PlayerSign.X, PlayerKind.Human);
            await channel.StartGameAsync(opponentConnection, "request-3", "started-session", "Carol");
            await channel.AddGameAsync(computerConnection, "request-4", "computer-session", "Dave", PlayerSign.X, PlayerKind.Computer);

            var games = await channel.GetGamesAsync();

            var sessionId = Assert.Single(games).SessionId;
            Assert.Equal("waiting-session", sessionId);
        }

        [Fact]
        public void TicTacToeChannelConfiguration_IsPublic()
        {
            var type = typeof(TicTacToeChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void TicTacToeChannelMetadata_IsPublic()
        {
            var type = typeof(TicTacToeChannelMetadata);
            Assert.True(type.IsPublic);
        }
    }
}
