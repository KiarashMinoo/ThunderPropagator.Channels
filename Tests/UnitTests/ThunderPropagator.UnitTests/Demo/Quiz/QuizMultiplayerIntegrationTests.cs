using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;
using ThunderPropagator.Channels.Demo.Quiz.Channel;
using ThunderPropagator.Channels.Demo.Quiz.Configuration;
using ThunderPropagator.Channels.Demo.Quiz.Feeders;
using ThunderPropagator.Channels.Demo.Quiz.Messages;

namespace ThunderPropagator.UnitTests.Demo.Quiz
{
    /// <summary>
    /// Issue #196: end-to-end multiplayer scenarios that combine several already individually-tested
    /// pieces (QuizChannel.Join/SubmitAnswer, QuizGameLoop's phase progression, QuizScoringEngine) into
    /// one continuous game, exercised the way a real deployment actually would rather than in isolation.
    /// Every other #196 scope bullet (state transitions, metadata, message validation, question bank,
    /// scoring including ties/duplicates/concurrency, pipeline tests, shutdown cancellation) is already
    /// covered elsewhere — see QuizGameLoopTests/QuizGameLoopAnswerIntegrationTests/QuizScoringEngineTests/
    /// QuizGameLoopStartNowTests (duplicate-phase-transition concurrency)/QuizFeederCancellationTests —
    /// this file's own job is specifically the gaps those leave: a full game driven through the real
    /// QuizChannel (not QuizGameLoop directly) with multiple joined players, mid-game join, disconnect/
    /// reconnect, concurrent answers resolved through the channel's own session/loop lookup (not just
    /// QuizScoringEngine in isolation), and two independently-running games proven never to contaminate
    /// each other's state. Every configured duration is shrunk to a millisecond and every phase is
    /// advanced explicitly via QuizGameLoop.Advance() — no wall-clock sleeps, no wait loops, fully
    /// deterministic and repeatable (#196's own AC).
    /// </summary>
    public sealed class QuizMultiplayerIntegrationTests
    {
        private const string GameId = "game-1";

        private static (QuizChannel Channel, QuizGameSessionStore SessionStore, QuizGameLoopRegistry LoopRegistry) CreateChannel(QuizChannelConfiguration? channelConfiguration = null)
        {
            var sessionStore = new QuizGameSessionStore();
            var loopRegistry = new QuizGameLoopRegistry();

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(QuizChannelConfiguration)).Returns(channelConfiguration ?? new QuizChannelConfiguration());
            serviceProvider.GetService(typeof(QuizGameSessionStore)).Returns(sessionStore);
            serviceProvider.GetService(typeof(QuizGameLoopRegistry)).Returns(loopRegistry);

            var channel = new QuizChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return (channel, sessionStore, loopRegistry);
        }

        private static QuizFeederConfiguration FastConfiguration() => new()
        {
            LobbyDuration = TimeSpan.FromMilliseconds(1),
            QuestionDuration = TimeSpan.FromMilliseconds(1),
            RevealingDuration = TimeSpan.FromMilliseconds(1),
            ScoreboardDuration = TimeSpan.FromMilliseconds(1)
        };

        // Mirrors QuizChannelSubmitAnswerTests/QuizChannelStartGameTests own setup: registers the loop
        // QuizChannel.SubmitAnswer resolves through QuizGameLoopRegistry, exactly as QuizFeeder would at
        // construction in a real deployment — but driven directly with Advance() here for determinism.
        private static QuizGameLoop CreateAndRegisterLoop(QuizGameSessionStore sessionStore, QuizGameLoopRegistry loopRegistry, string gameId, out QuizQuestionBank questionBank)
        {
            var session = sessionStore.GetOrCreateSession(gameId);
            questionBank = QuizQuestionBank.CreateDefault();
            var loop = new QuizGameLoop(session, questionBank, FastConfiguration());
            loopRegistry.Register(gameId, loop);
            return loop;
        }

        private static IConnectionInfo FakeConnection(string connectionId)
        {
            var connectionInfo = Substitute.For<IConnectionInfo>();
            connectionInfo.ConnectionId.Returns(connectionId);
            return connectionInfo;
        }

        private static int CorrectOptionIndexOf(QuizChannelFeederMessage message, QuizQuestionBank questionBank)
        {
            var question = questionBank.Questions.Single(candidate => candidate.Text == message.QuestionText);
            return message.Options.ToList().IndexOf(question.CorrectAnswer);
        }

        [Fact]
        public void FullGame_ThreePlayersFromLobbyToGameOver_ThroughTheChannel_ProducesACorrectFinalScoreboardAndWinner()
        {
            var (channel, sessionStore, loopRegistry) = CreateChannel(new QuizChannelConfiguration { MaxPlayers = 8 });
            var loop = CreateAndRegisterLoop(sessionStore, loopRegistry, GameId, out var questionBank);
            var session = sessionStore.TryGetSession(GameId)!;

            var aliceConnection = FakeConnection("conn-alice");
            var bobConnection = FakeConnection("conn-bob");
            var carolConnection = FakeConnection("conn-carol");
            channel.Join(aliceConnection, "req-1", GameId, "Alice");
            channel.Join(bobConnection, "req-2", GameId, "Bob");
            channel.Join(carolConnection, "req-3", GameId, "Carol");

            QuizChannelFeederMessage? last = null;
            var guard = 0;
            while (session.PhaseStateMachine.CurrentPhase != QuizPhase.GameOver)
            {
                last = loop.Advance();

                if (last is { Phase: QuizPhase.Question })
                {
                    var correctOptionIndex = CorrectOptionIndexOf(last, questionBank);
                    var wrongOptionIndex = (correctOptionIndex + 1) % last.Options.Count;

                    // Alice always answers correctly, Bob always wrong, Carol never answers at all —
                    // three distinct behaviors through the same real channel-level resolution path.
                    channel.SubmitAnswer(aliceConnection, GameId, last.QuestionIndex, correctOptionIndex);
                    channel.SubmitAnswer(bobConnection, GameId, last.QuestionIndex, wrongOptionIndex);
                }

                if (++guard > 10_000)
                    throw new TimeoutException("Game never reached GameOver.");
            }

            Assert.Equal(QuizPhase.GameOver, last!.Phase);
            Assert.Same(last, session.CurrentState);
            Assert.Equal("Alice", last.Winner);

            var aliceEntry = last.Scoreboard.Single(entry => entry.PlayerName == "Alice");
            Assert.Equal(questionBank.Count * (QuizScoringEngine.CorrectAnswerBasePoints + QuizScoringEngine.MaxResponseTimeBonus), aliceEntry.Score);
            Assert.DoesNotContain(last.Scoreboard, entry => entry.PlayerName == "Bob");
            Assert.DoesNotContain(last.Scoreboard, entry => entry.PlayerName == "Carol");
        }

        [Fact]
        public void MidGameJoin_APlayerJoiningAfterTheFirstQuestionWasScored_StartsWithNoScoreAndCanScoreOnLaterQuestions()
        {
            var (channel, sessionStore, loopRegistry) = CreateChannel();
            var loop = CreateAndRegisterLoop(sessionStore, loopRegistry, GameId, out var questionBank);

            var aliceConnection = FakeConnection("conn-alice");
            channel.Join(aliceConnection, "req-1", GameId, "Alice");

            var firstQuestion = loop.Advance()!; // Lobby -> Question(0)
            channel.SubmitAnswer(aliceConnection, GameId, firstQuestion.QuestionIndex, CorrectOptionIndexOf(firstQuestion, questionBank));
            loop.Advance(); // Question -> Revealing
            var afterFirstScoreboard = loop.Advance()!; // Revealing -> Scoreboard

            // Carol joins well after Lobby ended and after the first question has already been scored
            // (#187's own AllowMidGameJoin default) — the whole point of this test.
            var carolConnection = FakeConnection("conn-carol");
            var joinResult = channel.Join(carolConnection, "req-2", GameId, "Carol");

            Assert.False(joinResult.IsReconnect);
            Assert.DoesNotContain(afterFirstScoreboard.Scoreboard, entry => entry.PlayerName == "Carol");

            var secondQuestion = loop.Advance()!; // Scoreboard -> Question(1)
            channel.SubmitAnswer(carolConnection, GameId, secondQuestion.QuestionIndex, CorrectOptionIndexOf(secondQuestion, questionBank));
            loop.Advance(); // Question -> Revealing
            var afterSecondScoreboard = loop.Advance()!; // Revealing -> Scoreboard

            var carolEntry = afterSecondScoreboard.Scoreboard.Single(entry => entry.PlayerName == "Carol");
            Assert.Equal(QuizScoringEngine.CorrectAnswerBasePoints + QuizScoringEngine.MaxResponseTimeBonus, carolEntry.Score);
        }

        [Fact]
        public void DisconnectThenReconnectWithANewConnection_PreservesAccumulatedScoreAndCanKeepScoring()
        {
            var (channel, sessionStore, loopRegistry) = CreateChannel();
            var loop = CreateAndRegisterLoop(sessionStore, loopRegistry, GameId, out var questionBank);

            var bobConnection1 = FakeConnection("conn-bob-1");
            channel.Join(bobConnection1, "req-1", GameId, "Bob");

            var firstQuestion = loop.Advance()!; // Lobby -> Question(0)
            channel.SubmitAnswer(bobConnection1, GameId, firstQuestion.QuestionIndex, CorrectOptionIndexOf(firstQuestion, questionBank));
            loop.Advance(); // Question -> Revealing
            loop.Advance(); // Revealing -> Scoreboard

            var session = sessionStore.TryGetSession(GameId)!;
            Assert.True(session.Disconnect("conn-bob-1"));

            // A disconnected connection is no longer a resolvable joined player at all.
            Assert.Throws<QuizNotAJoinedPlayerException>(() => channel.SubmitAnswer(bobConnection1, GameId, 0, 0));

            var bobConnection2 = FakeConnection("conn-bob-2");
            var reconnectResult = channel.Join(bobConnection2, "req-2", GameId, "Bob");
            Assert.True(reconnectResult.IsReconnect);

            var secondQuestion = loop.Advance()!; // Scoreboard -> Question(1)
            channel.SubmitAnswer(bobConnection2, GameId, secondQuestion.QuestionIndex, CorrectOptionIndexOf(secondQuestion, questionBank));
            loop.Advance(); // Question -> Revealing
            var afterSecondScoreboard = loop.Advance()!; // Revealing -> Scoreboard

            var bobEntry = afterSecondScoreboard.Scoreboard.Single(entry => entry.PlayerName == "Bob");
            Assert.Equal(2 * (QuizScoringEngine.CorrectAnswerBasePoints + QuizScoringEngine.MaxResponseTimeBonus), bobEntry.Score);
        }

        // #196's own AC: "Concurrency tests are deterministic and repeatable" / "detects answer
        // leakage" — every thread submits under a distinct joined player through QuizChannel.SubmitAnswer
        // itself (session + loop-registry lookup included, not QuizScoringEngine directly), half
        // answering correctly and half incorrectly, so any accidental cross-player state sharing would
        // show up as an incorrect answerer scoring or a correct one not scoring.
        [Fact]
        public void SimultaneousAnswers_FromDistinctJoinedPlayers_ThroughTheChannel_EachScoresIndependentlyWithoutLeakage()
        {
            const int playerCount = 16;
            var (channel, sessionStore, loopRegistry) = CreateChannel(new QuizChannelConfiguration { MaxPlayers = playerCount });
            var loop = CreateAndRegisterLoop(sessionStore, loopRegistry, GameId, out var questionBank);

            var connections = Enumerable.Range(0, playerCount).Select(index => FakeConnection($"conn-{index}")).ToArray();
            for (var index = 0; index < playerCount; index++)
                channel.Join(connections[index], $"req-{index}", GameId, $"Player{index}");

            var question = loop.Advance()!; // Lobby -> Question
            var correctOptionIndex = CorrectOptionIndexOf(question, questionBank);
            var wrongOptionIndex = (correctOptionIndex + 1) % question.Options.Count;

            using var barrier = new Barrier(playerCount);
            var outcomes = new QuizAnswerOutcome[playerCount];
            var threads = Enumerable.Range(0, playerCount)
                .Select(index => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    var optionIndex = index % 2 == 0 ? correctOptionIndex : wrongOptionIndex;
                    outcomes[index] = channel.SubmitAnswer(connections[index], GameId, question.QuestionIndex, optionIndex);
                }))
                .ToArray();

            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                thread.Join();

            Assert.Equal(playerCount / 2, outcomes.Count(outcome => outcome == QuizAnswerOutcome.Correct));
            Assert.Equal(playerCount / 2, outcomes.Count(outcome => outcome == QuizAnswerOutcome.Incorrect));

            loop.Advance(); // Question -> Revealing
            var scoreboard = loop.Advance()!; // Revealing -> Scoreboard

            for (var index = 0; index < playerCount; index++)
            {
                var scoredCorrectly = scoreboard.Scoreboard.SingleOrDefault(entry => entry.PlayerName == $"Player{index}");
                if (index % 2 == 0)
                    Assert.Equal(QuizScoringEngine.CorrectAnswerBasePoints + QuizScoringEngine.MaxResponseTimeBonus, scoredCorrectly?.Score);
                else
                    Assert.Null(scoredCorrectly);
            }
        }

        // #196's own AC: "The suite detects ... cross-game state contamination." The same connection-id
        // string joins BOTH games under the same display name — the strongest available stress case for
        // accidental state sharing, since QuizGameSession/QuizScoringEngine/QuizGameLoop are all supposed
        // to be entirely independent per GameId even when nothing else about the two players differs.
        [Fact]
        public void TwoConcurrentGames_AdvancedInterleaved_NeverContaminateEachOthersState()
        {
            const string gameIdA = "game-A";
            const string gameIdB = "game-B";
            var (channel, sessionStore, loopRegistry) = CreateChannel();
            var loopA = CreateAndRegisterLoop(sessionStore, loopRegistry, gameIdA, out var questionBankA);
            var loopB = CreateAndRegisterLoop(sessionStore, loopRegistry, gameIdB, out var questionBankB);
            var sessionA = sessionStore.TryGetSession(gameIdA)!;
            var sessionB = sessionStore.TryGetSession(gameIdB)!;

            var aliceInA = FakeConnection("conn-alice");
            var aliceInB = FakeConnection("conn-alice");
            channel.Join(aliceInA, "req-a", gameIdA, "Alice");
            channel.Join(aliceInB, "req-b", gameIdB, "Alice");

            QuizChannelFeederMessage? lastA = null;
            QuizChannelFeederMessage? lastB = null;
            var guard = 0;
            while (sessionA.PhaseStateMachine.CurrentPhase != QuizPhase.GameOver || sessionB.PhaseStateMachine.CurrentPhase != QuizPhase.GameOver)
            {
                if (sessionA.PhaseStateMachine.CurrentPhase != QuizPhase.GameOver)
                {
                    lastA = loopA.Advance();
                    if (lastA is { Phase: QuizPhase.Question })
                        channel.SubmitAnswer(aliceInA, gameIdA, lastA.QuestionIndex, CorrectOptionIndexOf(lastA, questionBankA)); // Alice always right in A

                    Assert.Equal(gameIdA, lastA!.GameId);
                }

                if (sessionB.PhaseStateMachine.CurrentPhase != QuizPhase.GameOver)
                {
                    lastB = loopB.Advance();
                    if (lastB is { Phase: QuizPhase.Question })
                    {
                        var correctB = CorrectOptionIndexOf(lastB, questionBankB);
                        channel.SubmitAnswer(aliceInB, gameIdB, lastB.QuestionIndex, (correctB + 1) % lastB.Options.Count); // Alice always wrong in B
                    }

                    Assert.Equal(gameIdB, lastB!.GameId);
                }

                if (++guard > 20_000)
                    throw new TimeoutException("Both games never reached GameOver.");
            }

            Assert.Equal("Alice", lastA!.Winner);
            Assert.Equal(string.Empty, lastB!.Winner);

            Assert.Equal(questionBankA.Count * (QuizScoringEngine.CorrectAnswerBasePoints + QuizScoringEngine.MaxResponseTimeBonus), lastA.Scoreboard.Single().Score);
            Assert.Empty(lastB.Scoreboard);
        }
    }
}
