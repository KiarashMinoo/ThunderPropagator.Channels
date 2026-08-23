using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Exceptions;

namespace ThunderPropagator.UnitTests.Demo.Quiz
{
    /// <summary>
    /// Issue #191: covers QuizChannel.Join — the wire-agnostic core of Quiz/Join, exercised directly
    /// (an internal method, reachable from this test assembly) rather than through the full receive
    /// pipeline/DI stack, exactly like TicTacToeChannel's own channel-level game methods are tested
    /// elsewhere in this codebase. Every rejection path additionally asserts the session's player list
    /// is unchanged afterward — #191's own AC: "Invalid or disallowed joins do not mutate session
    /// state."
    /// </summary>
    public sealed class QuizChannelJoinTests
    {
        private const string GameId = "game-1";

        private static (QuizChannel Channel, QuizGameSessionStore SessionStore) CreateChannel()
        {
            var sessionStore = new QuizGameSessionStore();

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(QuizChannelConfiguration)).Returns(new QuizChannelConfiguration());
            serviceProvider.GetService(typeof(QuizGameSessionStore)).Returns(sessionStore);
            serviceProvider.GetService(typeof(QuizGameLoopRegistry)).Returns(new QuizGameLoopRegistry());

            var channel = new QuizChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return (channel, sessionStore);
        }

        private static IConnectionInfo FakeConnection(string connectionId)
        {
            var connectionInfo = Substitute.For<IConnectionInfo>();
            connectionInfo.ConnectionId.Returns(connectionId);
            return connectionInfo;
        }

        [Fact]
        public void Join_AValidPlayerIntoAnExistingLobby_Succeeds()
        {
            var (channel, sessionStore) = CreateChannel();
            sessionStore.GetOrCreateSession(GameId);

            var result = channel.Join(FakeConnection("conn-1"), "request-1", GameId, "Alice");

            Assert.Equal("Alice", result.PlayerName);
            Assert.False(result.IsReconnect);
            Assert.True(result.IsHost);
            Assert.NotNull(result.Subscription);
        }

        [Fact]
        public void Join_AddsTheJoiningConnectionAsAPlayerInTheSession()
        {
            var (channel, sessionStore) = CreateChannel();
            var session = sessionStore.GetOrCreateSession(GameId);

            channel.Join(FakeConnection("conn-1"), "request-1", GameId, "Alice");

            var player = Assert.Single(session.Players);
            Assert.Equal("Alice", player.PlayerName);
            Assert.Equal("conn-1", player.ConnectionId);
        }

        [Fact]
        public void Join_TrimsAndCollapsesWhitespaceInTheDisplayName()
        {
            var (channel, sessionStore) = CreateChannel();
            sessionStore.GetOrCreateSession(GameId);

            var result = channel.Join(FakeConnection("conn-1"), "request-1", GameId, "  Alice   Smith  ");

            Assert.Equal("Alice Smith", result.PlayerName);
        }

        [Fact]
        public void Join_UnknownGameId_ThrowsGameNotFound()
        {
            var (channel, _) = CreateChannel();

            var exception = Assert.Throws<QuizGameNotFoundException>(() => channel.Join(FakeConnection("conn-1"), "request-1", "does-not-exist", "Alice"));

            Assert.Equal("does-not-exist", exception.GameId);
        }

        [Fact]
        public void Join_UnknownGameId_DoesNotCreateASession()
        {
            var (channel, sessionStore) = CreateChannel();

            Assert.Throws<QuizGameNotFoundException>(() => channel.Join(FakeConnection("conn-1"), "request-1", "does-not-exist", "Alice"));

            Assert.Null(sessionStore.TryGetSession("does-not-exist"));
        }

        [Fact]
        public void Join_SameNameWhileStillConnected_ThrowsDuplicateJoin()
        {
            var (channel, sessionStore) = CreateChannel();
            sessionStore.GetOrCreateSession(GameId);
            channel.Join(FakeConnection("conn-1"), "request-1", GameId, "Alice");

            Assert.Throws<QuizDuplicateJoinException>(() => channel.Join(FakeConnection("conn-2"), "request-2", GameId, "Alice"));
        }

        [Fact]
        public void Join_SameNameWhileStillConnected_DoesNotChangeThePlayerList()
        {
            var (channel, sessionStore) = CreateChannel();
            var session = sessionStore.GetOrCreateSession(GameId);
            channel.Join(FakeConnection("conn-1"), "request-1", GameId, "Alice");

            Assert.Throws<QuizDuplicateJoinException>(() => channel.Join(FakeConnection("conn-2"), "request-2", GameId, "Alice"));

            var player = Assert.Single(session.Players);
            Assert.Equal("conn-1", player.ConnectionId);
        }

        [Fact]
        public void Join_SameNameAfterDisconnect_Reconnects()
        {
            var (channel, sessionStore) = CreateChannel();
            var session = sessionStore.GetOrCreateSession(GameId);
            channel.Join(FakeConnection("conn-1"), "request-1", GameId, "Alice");
            session.Disconnect("conn-1");

            var result = channel.Join(FakeConnection("conn-2"), "request-2", GameId, "Alice");

            Assert.True(result.IsReconnect);
            var player = Assert.Single(session.Players);
            Assert.Equal("conn-2", player.ConnectionId);
        }

        [Fact]
        public void Join_WhenGameIsAtMaxPlayers_RejectsANewPlayer()
        {
            var channelConfiguration = new QuizChannelConfiguration { MaxPlayers = 1 };
            var (channel, sessionStore) = CreateChannelWithConfiguration(channelConfiguration);
            sessionStore.GetOrCreateSession(GameId);
            channel.Join(FakeConnection("conn-1"), "request-1", GameId, "Alice");

            var exception = Assert.Throws<QuizGameFullException>(() => channel.Join(FakeConnection("conn-2"), "request-2", GameId, "Bob"));

            Assert.Equal(GameId, exception.GameId);
            Assert.Equal(1, exception.MaxPlayers);
        }

        [Fact]
        public void Join_WhenGameIsAtMaxPlayers_DoesNotAddTheRejectedPlayer()
        {
            var channelConfiguration = new QuizChannelConfiguration { MaxPlayers = 1 };
            var (channel, sessionStore) = CreateChannelWithConfiguration(channelConfiguration);
            var session = sessionStore.GetOrCreateSession(GameId);
            channel.Join(FakeConnection("conn-1"), "request-1", GameId, "Alice");

            Assert.Throws<QuizGameFullException>(() => channel.Join(FakeConnection("conn-2"), "request-2", GameId, "Bob"));

            var player = Assert.Single(session.Players);
            Assert.Equal("Alice", player.PlayerName);
        }

        [Fact]
        public void Join_WhenGameIsAtMaxPlayers_StillAllowsAReconnectOfAnExistingPlayer()
        {
            var channelConfiguration = new QuizChannelConfiguration { MaxPlayers = 1 };
            var (channel, sessionStore) = CreateChannelWithConfiguration(channelConfiguration);
            var session = sessionStore.GetOrCreateSession(GameId);
            channel.Join(FakeConnection("conn-1"), "request-1", GameId, "Alice");
            session.Disconnect("conn-1");

            var result = channel.Join(FakeConnection("conn-2"), "request-2", GameId, "Alice");

            Assert.True(result.IsReconnect);
        }

        [Fact]
        public void Join_WhenGameLeftLobby_SucceedsByDefault()
        {
            var (channel, sessionStore) = CreateChannel();
            var session = sessionStore.GetOrCreateSession(GameId);
            session.PhaseStateMachine.StartGame();

            var exception = Record.Exception(() => channel.Join(FakeConnection("conn-1"), "request-1", GameId, "Alice"));

            Assert.Null(exception);
        }

        [Fact]
        public void Join_WhenGameLeftLobbyAndMidGameJoinIsDisallowed_ThrowsNonLobbyJoinNotAllowed()
        {
            var channelConfiguration = new QuizChannelConfiguration { AllowMidGameJoin = false };
            var (channel, sessionStore) = CreateChannelWithConfiguration(channelConfiguration);
            var session = sessionStore.GetOrCreateSession(GameId);
            session.PhaseStateMachine.StartGame();

            var exception = Assert.Throws<QuizNonLobbyJoinNotAllowedException>(() => channel.Join(FakeConnection("conn-1"), "request-1", GameId, "Alice"));

            Assert.Equal(GameId, exception.GameId);
        }

        [Fact]
        public void Join_WhenGameLeftLobbyAndMidGameJoinIsDisallowed_DoesNotAddThePlayer()
        {
            var channelConfiguration = new QuizChannelConfiguration { AllowMidGameJoin = false };
            var (channel, sessionStore) = CreateChannelWithConfiguration(channelConfiguration);
            var session = sessionStore.GetOrCreateSession(GameId);
            session.PhaseStateMachine.StartGame();

            Assert.Throws<QuizNonLobbyJoinNotAllowedException>(() => channel.Join(FakeConnection("conn-1"), "request-1", GameId, "Alice"));

            Assert.Empty(session.Players);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Join_WithNullOrWhiteSpaceGameId_Throws(string? gameId)
        {
            var (channel, _) = CreateChannel();

            Assert.ThrowsAny<ArgumentException>(() => channel.Join(FakeConnection("conn-1"), "request-1", gameId!, "Alice"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Join_WithNullOrWhiteSpacePlayerName_Throws(string? playerName)
        {
            var (channel, sessionStore) = CreateChannel();
            sessionStore.GetOrCreateSession(GameId);

            Assert.ThrowsAny<ArgumentException>(() => channel.Join(FakeConnection("conn-1"), "request-1", GameId, playerName!));
        }

        [Fact]
        public void Join_WithAnOverlongDisplayName_ThrowsInvalidPlayerName()
        {
            var (channel, sessionStore) = CreateChannel();
            sessionStore.GetOrCreateSession(GameId);
            var overlongName = new string('a', QuizChannelFeederMessage.TextMaxLength + 1);

            Assert.Throws<QuizInvalidPlayerNameException>(() => channel.Join(FakeConnection("conn-1"), "request-1", GameId, overlongName));
        }

        [Fact]
        public void Join_WithAnOverlongDisplayName_DoesNotAddThePlayer()
        {
            var (channel, sessionStore) = CreateChannel();
            var session = sessionStore.GetOrCreateSession(GameId);
            var overlongName = new string('a', QuizChannelFeederMessage.TextMaxLength + 1);

            Assert.Throws<QuizInvalidPlayerNameException>(() => channel.Join(FakeConnection("conn-1"), "request-1", GameId, overlongName));

            Assert.Empty(session.Players);
        }

        private static (QuizChannel Channel, QuizGameSessionStore SessionStore) CreateChannelWithConfiguration(QuizChannelConfiguration channelConfiguration)
        {
            var sessionStore = new QuizGameSessionStore();

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(QuizChannelConfiguration)).Returns(channelConfiguration);
            serviceProvider.GetService(typeof(QuizGameSessionStore)).Returns(sessionStore);
            serviceProvider.GetService(typeof(QuizGameLoopRegistry)).Returns(new QuizGameLoopRegistry());

            var channel = new QuizChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return (channel, sessionStore);
        }
    }
}
