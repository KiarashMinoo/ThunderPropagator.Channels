using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Game
{
    /// <summary>
    /// Issue #190: covers QuizGameLoop.SubmitAnswer's own window enforcement (tied to this loop's
    /// actual phase, on top of QuizScoringEngineTests' own coverage of the scoring rules in isolation),
    /// and that a Scoreboard broadcasts after every reveal and a Winner is exposed at GameOver.
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

        [Fact]
        public void SubmitAnswer_BeforeTheGameHasStarted_ReturnsWindowClosed()
        {
            var loop = CreateLoop(out _, out _);

            var outcome = loop.SubmitAnswer("Alice", "anything");

            Assert.Equal(QuizAnswerOutcome.WindowClosed, outcome);
        }

        [Fact]
        public void SubmitAnswer_WhileQuestionIsOpen_WithTheCorrectAnswer_ReturnsCorrect()
        {
            var loop = CreateLoop(out _, out var questionBank);
            var message = loop.Advance()!; // Lobby -> Question
            var correctAnswer = questionBank.Questions.Single(q => q.Text == message.QuestionText).CorrectAnswer;

            var outcome = loop.SubmitAnswer("Alice", correctAnswer);

            Assert.Equal(QuizAnswerOutcome.Correct, outcome);
        }

        [Fact]
        public void SubmitAnswer_AfterTheQuestionWindowCloses_ReturnsWindowClosed()
        {
            var feederConfiguration = new QuizFeederConfiguration { QuestionDuration = TimeSpan.FromSeconds(1) };
            var loop = CreateLoop(out _, out _, feederConfiguration);
            loop.Advance(); // Lobby -> Question
            loop.Advance(); // Question -> Revealing (1s tick exhausts it)

            var outcome = loop.SubmitAnswer("Alice", "anything");

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

            Assert.Equal(QuizAnswerOutcome.WindowClosed, loop.SubmitAnswer("Alice", "anything"));
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
            var correctAnswer = questionBank.Questions.Single(q => q.Text == questionMessage.QuestionText).CorrectAnswer;

            loop.SubmitAnswer("Alice", correctAnswer);
            var revealing = loop.Advance(); // Question -> Revealing
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
                {
                    var correctAnswer = questionBank.Questions.Single(q => q.Text == last.QuestionText).CorrectAnswer;
                    loop.SubmitAnswer("Alice", correctAnswer);
                }

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
                    var correctAnswer = questionBank.Questions.Single(q => q.Text == last.QuestionText).CorrectAnswer;
                    loop.SubmitAnswer("Amy", correctAnswer);
                    loop.SubmitAnswer("Zoe", correctAnswer);
                }

                if (++guard > 10_000)
                    throw new TimeoutException("Game never reached GameOver.");
            }

            Assert.Equal("Amy, Zoe", last!.Winner);
        }
    }
}
