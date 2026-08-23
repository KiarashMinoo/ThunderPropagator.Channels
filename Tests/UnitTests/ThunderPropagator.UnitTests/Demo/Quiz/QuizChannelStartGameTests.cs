using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.UnitTests.Demo.Quiz
{
    /// <summary>
    /// Issue #193: covers QuizChannel.StartGame — host authorization resolved from server-side
    /// connection state (never a caller-supplied identity), minimum-player enforcement, the
    /// already-started/duplicate-request contract, and that a successful start actually broadcasts
    /// (not merely records) the new state, since this transition — unlike Join/SubmitAnswer — is not
    /// otherwise driven by QuizFeeder's own tick loop.
    /// </summary>
    public sealed class QuizChannelStartGameTests
    {
        private const string GameId = "game-1";

        private static IConnectionInfo FakeConnection(string connectionId)
        {
            var connectionInfo = Substitute.For<IConnectionInfo>();
            connectionInfo.ConnectionId.Returns(connectionId);
            return connectionInfo;
        }

        private static (QuizChannel Channel, QuizGameSessionStore SessionStore, QuizGameLoopRegistry GameLoopRegistry) CreateChannelWithSession(QuizChannelConfiguration? channelConfiguration = null)
        {
            var sessionStore = new QuizGameSessionStore();
            var gameLoopRegistry = new QuizGameLoopRegistry();
            var session = sessionStore.GetOrCreateSession(GameId);
            var gameLoop = new QuizGameLoop(session, QuizQuestionBank.CreateDefault(), new QuizFeederConfiguration());
            gameLoopRegistry.Register(GameId, gameLoop);

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(QuizChannelConfiguration)).Returns(channelConfiguration ?? new QuizChannelConfiguration { MinPlayers = 1 });
            serviceProvider.GetService(typeof(QuizGameSessionStore)).Returns(sessionStore);
            serviceProvider.GetService(typeof(QuizGameLoopRegistry)).Returns(gameLoopRegistry);

            var channel = new QuizChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return (channel, sessionStore, gameLoopRegistry);
        }

        [Fact]
        public void StartGame_ByTheHost_ReturnsStarted()
        {
            var (channel, sessionStore, _) = CreateChannelWithSession();
            var host = FakeConnection("conn-host");
            channel.Join(host, "request-1", GameId, "Alice");

            var outcome = channel.StartGame(host, GameId);

            Assert.Equal(QuizStartOutcome.Started, outcome);
        }

        [Fact]
        public void StartGame_ByTheHost_TransitionsTheSessionOutOfLobby()
        {
            var (channel, sessionStore, _) = CreateChannelWithSession();
            var host = FakeConnection("conn-host");
            channel.Join(host, "request-1", GameId, "Alice");

            channel.StartGame(host, GameId);

            Assert.Equal(QuizPhase.Question, sessionStore.TryGetSession(GameId)!.PhaseStateMachine.CurrentPhase);
        }

        [Fact]
        public void StartGame_FromAConnectionThatNeverJoined_ThrowsNotAJoinedPlayer()
        {
            var (channel, _, _) = CreateChannelWithSession();

            var exception = Assert.Throws<QuizNotAJoinedPlayerException>(() => channel.StartGame(FakeConnection("conn-1"), GameId));

            Assert.Equal(GameId, exception.GameId);
        }

        [Fact]
        public void StartGame_ByANonHostJoinedPlayer_ThrowsNotTheHost()
        {
            var (channel, _, _) = CreateChannelWithSession(new QuizChannelConfiguration { MinPlayers = 1, MaxPlayers = 8 });
            var host = FakeConnection("conn-host");
            var guest = FakeConnection("conn-guest");
            channel.Join(host, "request-1", GameId, "Alice");
            channel.Join(guest, "request-2", GameId, "Bob");

            var exception = Assert.Throws<QuizNotTheHostException>(() => channel.StartGame(guest, GameId));

            Assert.Equal(GameId, exception.GameId);
        }

        [Fact]
        public void StartGame_ByANonHostJoinedPlayer_DoesNotChangeThePhase()
        {
            var (channel, sessionStore, _) = CreateChannelWithSession(new QuizChannelConfiguration { MinPlayers = 1, MaxPlayers = 8 });
            var host = FakeConnection("conn-host");
            var guest = FakeConnection("conn-guest");
            channel.Join(host, "request-1", GameId, "Alice");
            channel.Join(guest, "request-2", GameId, "Bob");

            Assert.Throws<QuizNotTheHostException>(() => channel.StartGame(guest, GameId));

            Assert.Equal(QuizPhase.Lobby, sessionStore.TryGetSession(GameId)!.PhaseStateMachine.CurrentPhase);
        }

        [Fact]
        public void StartGame_UnknownGameId_ThrowsGameNotFound()
        {
            var (channel, _, _) = CreateChannelWithSession();

            var exception = Assert.Throws<QuizGameNotFoundException>(() => channel.StartGame(FakeConnection("conn-1"), "does-not-exist"));

            Assert.Equal("does-not-exist", exception.GameId);
        }

        [Fact]
        public void StartGame_WithFewerThanMinPlayersConnected_ThrowsNotEnoughPlayers()
        {
            var (channel, _, _) = CreateChannelWithSession(new QuizChannelConfiguration { MinPlayers = 2 });
            var host = FakeConnection("conn-host");
            channel.Join(host, "request-1", GameId, "Alice");

            var exception = Assert.Throws<QuizNotEnoughPlayersException>(() => channel.StartGame(host, GameId));

            Assert.Equal(GameId, exception.GameId);
            Assert.Equal(2, exception.MinPlayers);
            Assert.Equal(1, exception.ConnectedPlayers);
        }

        [Fact]
        public void StartGame_WithFewerThanMinPlayersConnected_DoesNotChangeThePhase()
        {
            var (channel, sessionStore, _) = CreateChannelWithSession(new QuizChannelConfiguration { MinPlayers = 2 });
            var host = FakeConnection("conn-host");
            channel.Join(host, "request-1", GameId, "Alice");

            Assert.Throws<QuizNotEnoughPlayersException>(() => channel.StartGame(host, GameId));

            Assert.Equal(QuizPhase.Lobby, sessionStore.TryGetSession(GameId)!.PhaseStateMachine.CurrentPhase);
        }

        [Fact]
        public void StartGame_WithExactlyMinPlayersConnected_Succeeds()
        {
            var (channel, _, _) = CreateChannelWithSession(new QuizChannelConfiguration { MinPlayers = 2, MaxPlayers = 8 });
            var host = FakeConnection("conn-host");
            var guest = FakeConnection("conn-guest");
            channel.Join(host, "request-1", GameId, "Alice");
            channel.Join(guest, "request-2", GameId, "Bob");

            var outcome = channel.StartGame(host, GameId);

            Assert.Equal(QuizStartOutcome.Started, outcome);
        }

        [Fact]
        public void StartGame_CalledAgainAfterAlreadyStarted_ReturnsAlreadyStarted()
        {
            var (channel, _, _) = CreateChannelWithSession();
            var host = FakeConnection("conn-host");
            channel.Join(host, "request-1", GameId, "Alice");
            channel.StartGame(host, GameId);

            var outcome = channel.StartGame(host, GameId);

            Assert.Equal(QuizStartOutcome.AlreadyStarted, outcome);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void StartGame_WithNullOrWhiteSpaceGameId_Throws(string? gameId)
        {
            var (channel, _, _) = CreateChannelWithSession();

            Assert.ThrowsAny<ArgumentException>(() => channel.StartGame(FakeConnection("conn-1"), gameId!));
        }

        // Issue #193's own AC: "Concurrent requests create one running loop" — every thread is the
        // exact same authorized host racing the exact same start; exactly one may observe Started.
        [Fact]
        public void StartGame_CalledConcurrentlyByTheSameHost_ExactlyOneReportsStarted()
        {
            const int threadCount = 16;
            var (channel, _, _) = CreateChannelWithSession();
            var host = FakeConnection("conn-host");
            channel.Join(host, "request-1", GameId, "Alice");

            using var barrier = new Barrier(threadCount);
            var outcomes = new QuizStartOutcome[threadCount];

            var threads = Enumerable.Range(0, threadCount)
                .Select(index => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    outcomes[index] = channel.StartGame(host, GameId);
                }))
                .ToArray();

            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                thread.Join();

            Assert.Equal(1, outcomes.Count(outcome => outcome == QuizStartOutcome.Started));
            Assert.Equal(threadCount - 1, outcomes.Count(outcome => outcome == QuizStartOutcome.AlreadyStarted));
        }
    }
}
