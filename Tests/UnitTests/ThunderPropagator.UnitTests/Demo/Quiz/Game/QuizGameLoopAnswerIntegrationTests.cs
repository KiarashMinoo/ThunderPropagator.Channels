using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Game
{
    /// <summary>
    /// Issue #190/#192: covers QuizGameLoop.SubmitAnswer's own window/staleness/option-index
    /// enforcement (tied to this loop's actual phase and question, on top of QuizScoringEngineTests'
    /// own coverage of the scoring rules in isolation), and that a Scoreboard broadcasts after every
    /// reveal and a Winner is exposed at GameOver.
    /// </summary>
    public sealed class QuizGameLoopAnswerIntegrationTests
    {
        private const string GameId = "game-1";

        private static QuizGameLoop CreateLoop(out QuizGameSession session, out QuizQuestionBank questionBank, QuizFeederConfiguration? feederConfiguration = null)
        {
            session = new QuizGameSession(GameId);
            questionBank = QuizQuestionBank.CreateDefault();
            return new QuizGameLoop(session, questionBank, feederConfiguration ?? new QuizFeederConfiguration());
        }

        private static int CorrectOptionIndexOf(QuizChannelFeederMessage message, QuizQuestionBank questionBank)
        {
            var question = questionBank.Questions.Single(q => q.Text == message.QuestionText);
            return message.Options.ToList().IndexOf(question.CorrectAnswer);
        }

        [Fact]
        public void SubmitAnswer_BeforeTheGameHasStarted_ReturnsWindowClosed()
        {
            var loop = CreateLoop(out _, out _);

            var outcome = loop.SubmitAnswer("Alice", 0, 0);

            Assert.Equal(QuizAnswerOutcome.WindowClosed, outcome);
        }

        [Fact]
        public void SubmitAnswer_WhileQuestionIsOpen_WithTheCorrectAnswer_ReturnsCorrect()
        {
            var loop = CreateLoop(out _, out var questionBank);
            var message = loop.Advance()!; // Lobby -> Question

            var outcome = loop.SubmitAnswer("Alice", message.QuestionIndex, CorrectOptionIndexOf(message, questionBank));

            Assert.Equal(QuizAnswerOutcome.Correct, outcome);
        }

        [Fact]
        public void SubmitAnswer_AfterTheQuestionWindowCloses_ReturnsWindowClosed()
        {
            var feederConfiguration = new QuizFeederConfiguration { QuestionDuration = TimeSpan.FromSeconds(1) };
            var loop = CreateLoop(out _, out _, feederConfiguration);
            loop.Advance(); // Lobby -> Question
            loop.Advance(); // Question -> Revealing (1s tick exhausts it)

            var outcome = loop.SubmitAnswer("Alice", 0, 0);

            Assert.Equal(QuizAnswerOutcome.WindowClosed, outcome);
        }

        [Fact]
        public void SubmitAnswer_DuringRevealingOrScoreboard_ReturnsWindowClosed()
        {
            var feederConfiguration = new QuizFeederConfiguration { QuestionDuration = TimeSpan.FromSeconds(1) };
            var loop = CreateLoop(out _, out _, feederConfiguration);
            loop.Advance(); // Lobby -> Question
            loop.Advance(); // Question -> Revealing
            loop.Advance(); // Revealing -> Scoreboard

            Assert.Equal(QuizAnswerOutcome.WindowClosed, loop.SubmitAnswer("Alice", 0, 0));
        }

        [Fact]
        public void SubmitAnswer_WithAQuestionIndexOtherThanTheOneCurrentlyOpen_ReturnsStale()
        {
            var loop = CreateLoop(out _, out _);
            var message = loop.Advance()!; // Lobby -> Question (index 0)

            var outcome = loop.SubmitAnswer("Alice", message.QuestionIndex + 1, 0);

            Assert.Equal(QuizAnswerOutcome.Stale, outcome);
        }

        [Fact]
        public void SubmitAnswer_WithAQuestionIndexOtherThanTheOneCurrentlyOpen_AwardsNoScore()
        {
            var loop = CreateLoop(out _, out var questionBank);
            var message = loop.Advance()!; // Lobby -> Question

            loop.SubmitAnswer("Alice", message.QuestionIndex + 1, CorrectOptionIndexOf(message, questionBank));

            var revealing = loop.Advance();
            Assert.Empty(revealing!.Scoreboard);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public void SubmitAnswer_WithAnOptionIndexOutOfRange_ReturnsInvalid(int outOfRangeOptionIndex)
        {
            var loop = CreateLoop(out _, out _);
            var message = loop.Advance()!; // Lobby -> Question (every default question has < 5 options)

            var outcome = loop.SubmitAnswer("Alice", message.QuestionIndex, outOfRangeOptionIndex);

            Assert.Equal(QuizAnswerOutcome.Invalid, outcome);
        }

        [Fact]
        public void SubmitAnswer_WithAnOptionIndexOutOfRange_DoesNotConsumeTheAnswerSlot()
        {
            var loop = CreateLoop(out _, out var questionBank);
            var message = loop.Advance()!; // Lobby -> Question
            loop.SubmitAnswer("Alice", message.QuestionIndex, -1); // invalid — must not count as an attempt

            var outcome = loop.SubmitAnswer("Alice", message.QuestionIndex, CorrectOptionIndexOf(message, questionBank));

            Assert.Equal(QuizAnswerOutcome.Correct, outcome);
        }

        [Fact]
        public void Message_BeforeAnyReveal_HasAnEmptyScoreboard()
        {
            var loop = CreateLoop(out _, out _);

            var message = loop.Advance(); // Lobby -> Question

            Assert.Empty(message!.Scoreboard);
        }

        [Fact]
        public void Message_AfterReveal_BroadcastsAScoreboardReflectingTheAnsweredQuestion()
        {
            var feederConfiguration = new QuizFeederConfiguration { QuestionDuration = TimeSpan.FromSeconds(1) };
            var loop = CreateLoop(out _, out var questionBank, feederConfiguration);
            var questionMessage = loop.Advance()!; // Lobby -> Question

            loop.SubmitAnswer("Alice", questionMessage.QuestionIndex, CorrectOptionIndexOf(questionMessage, questionBank));
            loop.Advance(); // Question -> Revealing
            var scoreboard = loop.Advance(); // Revealing -> Scoreboard

            Assert.Equal(QuizPhase.Scoreboard, scoreboard!.Phase);
            var entry = Assert.Single(scoreboard.Scoreboard);
            Assert.Equal("Alice", entry.PlayerName);
            Assert.Equal(QuizScoringEngine.CorrectAnswerBasePoints + QuizScoringEngine.MaxResponseTimeBonus, entry.Score);
        }

        [Fact]
        public void FullGame_WithASoleCorrectPlayer_ExposesThemAsTheWinnerAtGameOver()
        {
            var feederConfiguration = new QuizFeederConfiguration
            {
                LobbyDuration = TimeSpan.FromMilliseconds(1),
                QuestionDuration = TimeSpan.FromMilliseconds(1),
                RevealingDuration = TimeSpan.FromMilliseconds(1),
                ScoreboardDuration = TimeSpan.FromMilliseconds(1)
            };
            var loop = CreateLoop(out var session, out var questionBank, feederConfiguration);

            QuizChannelFeederMessage? last = null;
            var guard = 0;
            while (session.PhaseStateMachine.CurrentPhase != QuizPhase.GameOver)
            {
                last = loop.Advance();

                if (last is { Phase: QuizPhase.Question })
                    loop.SubmitAnswer("Alice", last.QuestionIndex, CorrectOptionIndexOf(last, questionBank));

                if (++guard > 10_000)
                    throw new TimeoutException("Game never reached GameOver.");
            }

            Assert.Equal("Alice", last!.Winner);
            var entry = Assert.Single(last.Scoreboard);
            Assert.Equal("Alice", entry.PlayerName);
            Assert.Equal(questionBank.Count * (QuizScoringEngine.CorrectAnswerBasePoints + QuizScoringEngine.MaxResponseTimeBonus), entry.Score);
        }

        [Fact]
        public void FullGame_WithATiedFinalScore_ExposesBothWinnersAtGameOver()
        {
            var feederConfiguration = new QuizFeederConfiguration
            {
                LobbyDuration = TimeSpan.FromMilliseconds(1),
                QuestionDuration = TimeSpan.FromMilliseconds(1),
                RevealingDuration = TimeSpan.FromMilliseconds(1),
                ScoreboardDuration = TimeSpan.FromMilliseconds(1)
            };
            var loop = CreateLoop(out var session, out var questionBank, feederConfiguration);

            QuizChannelFeederMessage? last = null;
            var guard = 0;
            while (session.PhaseStateMachine.CurrentPhase != QuizPhase.GameOver)
            {
                last = loop.Advance();

                if (last is { Phase: QuizPhase.Question })
                {
                    var correctOptionIndex = CorrectOptionIndexOf(last, questionBank);
                    loop.SubmitAnswer("Amy", last.QuestionIndex, correctOptionIndex);
                    loop.SubmitAnswer("Zoe", last.QuestionIndex, correctOptionIndex);
                }

                if (++guard > 10_000)
                    throw new TimeoutException("Game never reached GameOver.");
            }

            Assert.Equal("Amy, Zoe", last!.Winner);
        }
    }
}
