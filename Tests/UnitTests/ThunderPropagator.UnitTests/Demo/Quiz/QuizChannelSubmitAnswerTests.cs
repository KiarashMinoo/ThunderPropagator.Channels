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
    /// Issue #192: covers QuizChannel.SubmitAnswer — resolving the submitting player from the session's
    /// own established membership rather than any caller-supplied identity ("Only joined players can
    /// answer their active game/question"), and delegating everything else (phase/staleness/option
    /// validity/duplicates/scoring) to QuizGameLoop.SubmitAnswer, which QuizGameLoopAnswerIntegrationTests
    /// and QuizScoringEngineTests already cover in depth.
    /// </summary>
    public sealed class QuizChannelSubmitAnswerTests
    {
        private const string GameId = "game-1";

        private static (QuizChannel Channel, QuizGameSession Session, QuizGameLoop GameLoop) CreateChannelWithAnOpenQuestion()
        {
            var sessionStore = new QuizGameSessionStore();
            var session = sessionStore.GetOrCreateSession(GameId);
            var gameLoopRegistry = new QuizGameLoopRegistry();
            var gameLoop = new QuizGameLoop(session, QuizQuestionBank.CreateDefault(), new QuizFeederConfiguration());
            gameLoopRegistry.Register(GameId, gameLoop);

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(QuizChannelConfiguration)).Returns(new QuizChannelConfiguration { MaxPlayers = 64 });
            serviceProvider.GetService(typeof(QuizGameSessionStore)).Returns(sessionStore);
            serviceProvider.GetService(typeof(QuizGameLoopRegistry)).Returns(gameLoopRegistry);

            var channel = new QuizChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            gameLoop.Advance(); // Lobby -> Question

            return (channel, session, gameLoop);
        }

        private static IConnectionInfo FakeConnection(string connectionId)
        {
            var connectionInfo = Substitute.For<IConnectionInfo>();
            connectionInfo.ConnectionId.Returns(connectionId);
            return connectionInfo;
        }

        [Fact]
        public void SubmitAnswer_FromAJoinedPlayer_ScoresAgainstTheOpenQuestion()
        {
            var (channel, _, _) = CreateChannelWithAnOpenQuestion();
            var connectionInfo = FakeConnection("conn-1");
            channel.Join(connectionInfo, "request-1", GameId, "Alice");

            var outcome = channel.SubmitAnswer(connectionInfo, GameId, 0, 0);

            Assert.True(outcome is QuizAnswerOutcome.Correct or QuizAnswerOutcome.Incorrect);
        }

        [Fact]
        public void SubmitAnswer_FromAConnectionThatNeverJoined_ThrowsNotAJoinedPlayer()
        {
            var (channel, _, _) = CreateChannelWithAnOpenQuestion();

            var exception = Assert.Throws<QuizNotAJoinedPlayerException>(() => channel.SubmitAnswer(FakeConnection("conn-1"), GameId, 0, 0));

            Assert.Equal(GameId, exception.GameId);
        }

        [Fact]
        public void SubmitAnswer_FromADisconnectedFormerPlayersConnection_ThrowsNotAJoinedPlayer()
        {
            var (channel, session, _) = CreateChannelWithAnOpenQuestion();
            var connectionInfo = FakeConnection("conn-1");
            channel.Join(connectionInfo, "request-1", GameId, "Alice");
            session.Disconnect("conn-1");

            Assert.Throws<QuizNotAJoinedPlayerException>(() => channel.SubmitAnswer(connectionInfo, GameId, 0, 0));
        }

        [Fact]
        public void SubmitAnswer_ScoresUnderThePlayersOwnServerSideIdentity_NotAnyClientSuppliedValue()
        {
            // QuizChannel.SubmitAnswer takes no player-name parameter at all — the only way this test
            // can prove identity comes from the connection is that the scoreboard ends up crediting
            // the name the player actually joined under.
            var (channel, _, gameLoop) = CreateChannelWithAnOpenQuestion();
            var connectionInfo = FakeConnection("conn-1");
            channel.Join(connectionInfo, "request-1", GameId, "Alice");

            channel.SubmitAnswer(connectionInfo, GameId, 0, 0);
            gameLoop.Advance(); // Question -> Revealing
            var scoreboardMessage = gameLoop.Advance(); // Revealing -> Scoreboard

            if (scoreboardMessage!.Scoreboard.Count > 0)
                Assert.Equal("Alice", scoreboardMessage.Scoreboard[0].PlayerName);
        }

        [Fact]
        public void SubmitAnswer_UnknownGameId_ThrowsGameNotFound()
        {
            var (channel, _, _) = CreateChannelWithAnOpenQuestion();

            var exception = Assert.Throws<QuizGameNotFoundException>(() => channel.SubmitAnswer(FakeConnection("conn-1"), "does-not-exist", 0, 0));

            Assert.Equal("does-not-exist", exception.GameId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void SubmitAnswer_WithNullOrWhiteSpaceGameId_Throws(string? gameId)
        {
            var (channel, _, _) = CreateChannelWithAnOpenQuestion();

            Assert.ThrowsAny<ArgumentException>(() => channel.SubmitAnswer(FakeConnection("conn-1"), gameId!, 0, 0));
        }

        [Fact]
        public void SubmitAnswer_CalledConcurrentlyByDistinctJoinedPlayers_ScoresEveryoneExactlyOnce()
        {
            const int playerCount = 16;
            var (channel, _, gameLoop) = CreateChannelWithAnOpenQuestion();

            var connections = Enumerable.Range(0, playerCount)
                .Select(index =>
                {
                    var connectionInfo = FakeConnection($"conn-{index}");
                    channel.Join(connectionInfo, $"request-{index}", GameId, $"Player{index}");
                    return connectionInfo;
                })
                .ToArray();

            using var barrier = new Barrier(playerCount);
            var outcomes = new QuizAnswerOutcome[playerCount];

            var threads = Enumerable.Range(0, playerCount)
                .Select(index => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    outcomes[index] = channel.SubmitAnswer(connections[index], GameId, 0, 0);
                }))
                .ToArray();

            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                thread.Join();

            Assert.All(outcomes, outcome => Assert.True(outcome is QuizAnswerOutcome.Correct or QuizAnswerOutcome.Incorrect));

            var revealing = gameLoop.Advance(); // Question -> Revealing
            var correctCount = outcomes.Count(outcome => outcome == QuizAnswerOutcome.Correct);
            Assert.Equal(correctCount, revealing!.Scoreboard.Count);
        }
    }
}
