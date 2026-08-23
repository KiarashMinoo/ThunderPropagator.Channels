using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Game
{
    /// <summary>
    /// Issue #189: covers QuizGameLoop's phase progression — Lobby → Question countdown → Revealing →
    /// Scoreboard → next Question or GameOver — entirely synchronously (NextDelay/Advance never
    /// actually wait), which is exactly what makes this deterministic without any real elapsed time or
    /// time-abstraction plumbing (the AC's own "controllable time abstraction or tick strategy for
    /// deterministic tests"). QuizFeederCancellationTests/QuizFeederSubscriptionGatingTests cover the
    /// real, cancellable delay QuizFeeder layers on top of NextDelay.
    /// </summary>
    public sealed class QuizGameLoopTests
    {
        private const string GameId = "game-1";

        private static QuizGameLoop CreateLoop(out QuizGameSession session, out QuizQuestionBank questionBank, QuizFeederConfiguration? feederConfiguration = null)
        {
            session = new QuizGameSession(GameId);
            questionBank = QuizQuestionBank.CreateDefault();
            return new QuizGameLoop(session, questionBank, feederConfiguration ?? new QuizFeederConfiguration());
        }

        [Fact]
        public void Constructor_WithNullSession_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new QuizGameLoop(null!, QuizQuestionBank.CreateDefault(), new QuizFeederConfiguration()));
        }

        [Fact]
        public void Constructor_WithNullQuestionBank_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new QuizGameLoop(new QuizGameSession(GameId), null!, new QuizFeederConfiguration()));
        }

        [Fact]
        public void Constructor_WithNullFeederConfiguration_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new QuizGameLoop(new QuizGameSession(GameId), QuizQuestionBank.CreateDefault(), null!));
        }

        [Fact]
        public void NextDelay_WhileInLobby_IsTheConfiguredLobbyDuration()
        {
            var feederConfiguration = new QuizFeederConfiguration { LobbyDuration = TimeSpan.FromSeconds(7) };
            var loop = CreateLoop(out _, out _, feederConfiguration);

            Assert.Equal(TimeSpan.FromSeconds(7), loop.NextDelay);
        }

        [Fact]
        public void Advance_FromLobby_TransitionsToQuestionAndPopulatesTheFirstQuestion()
        {
            var loop = CreateLoop(out var session, out var questionBank);

            var message = loop.Advance();

            Assert.NotNull(message);
            Assert.Equal(QuizPhase.Question, message.Phase);
            Assert.Equal(QuizPhase.Question, session.PhaseStateMachine.CurrentPhase);
            Assert.Equal(0, message.QuestionIndex);
            Assert.Equal(questionBank.Count, message.TotalQuestions);
            Assert.Contains(questionBank.Questions, question => question.Text == message.QuestionText);
            Assert.NotEmpty(message.Options);
        }

        [Fact]
        public void Advance_FromLobby_RecordsTheReturnedMessageAsTheSessionsCurrentState()
        {
            var loop = CreateLoop(out var session, out _);

            var message = loop.Advance();

            Assert.Same(message, session.CurrentState);
        }

        [Fact]
        public void Advance_FromLobby_SetsTimeRemainingToTheFullConfiguredQuestionDuration()
        {
            var feederConfiguration = new QuizFeederConfiguration { QuestionDuration = TimeSpan.FromSeconds(12) };
            var loop = CreateLoop(out _, out _, feederConfiguration);

            var message = loop.Advance();

            Assert.Equal(12, message!.TimeRemaining);
        }

        [Fact]
        public void NextDelay_DuringQuestion_IsCappedToAtMostOneSecond()
        {
            var feederConfiguration = new QuizFeederConfiguration { QuestionDuration = TimeSpan.FromSeconds(15) };
            var loop = CreateLoop(out _, out _, feederConfiguration);
            loop.Advance(); // Lobby -> Question, 15s remaining

            Assert.Equal(TimeSpan.FromSeconds(1), loop.NextDelay);
        }

        [Fact]
        public void NextDelay_NearTheEndOfAQuestion_IsCappedToTheRemainder()
        {
            var feederConfiguration = new QuizFeederConfiguration { QuestionDuration = TimeSpan.FromMilliseconds(1500) };
            var loop = CreateLoop(out _, out _, feederConfiguration);
            loop.Advance(); // Lobby -> Question, 1500ms remaining
            loop.Advance(); // one 1s tick consumed, 500ms remaining

            Assert.Equal(TimeSpan.FromMilliseconds(500), loop.NextDelay);
        }

        [Fact]
        public void Advance_RepeatedlyDuringQuestion_CountsDownWithoutSkippingOrRepeatingASecond()
        {
            var feederConfiguration = new QuizFeederConfiguration { QuestionDuration = TimeSpan.FromSeconds(3) };
            var loop = CreateLoop(out _, out _, feederConfiguration);

            var first = loop.Advance(); // Lobby -> Question
            var second = loop.Advance(); // 3 -> 2
            var third = loop.Advance(); // 2 -> 1

            Assert.Equal(3, first!.TimeRemaining);
            Assert.Equal(QuizPhase.Question, second!.Phase);
            Assert.Equal(2, second.TimeRemaining);
            Assert.Equal(QuizPhase.Question, third!.Phase);
            Assert.Equal(1, third.TimeRemaining);
        }

        [Fact]
        public void Advance_WhenAQuestionsCountdownReachesZero_TransitionsToRevealingRatherThanShowingZero()
        {
            var feederConfiguration = new QuizFeederConfiguration { QuestionDuration = TimeSpan.FromSeconds(2) };
            var loop = CreateLoop(out var session, out _, feederConfiguration);

            loop.Advance(); // Lobby -> Question, 2s remaining
            loop.Advance(); // 2 -> 1
            var revealing = loop.Advance(); // 1 -> 0, transitions

            Assert.Equal(QuizPhase.Revealing, revealing!.Phase);
            Assert.Equal(QuizPhase.Revealing, session.PhaseStateMachine.CurrentPhase);
            Assert.Equal(0, revealing.TimeRemaining);
        }

        [Fact]
        public void Advance_DuringRevealing_ExposesTheCorrectAnswer()
        {
            var feederConfiguration = new QuizFeederConfiguration { QuestionDuration = TimeSpan.FromSeconds(1) };
            var loop = CreateLoop(out _, out var questionBank, feederConfiguration);

            loop.Advance(); // Lobby -> Question
            var revealing = loop.Advance(); // Question -> Revealing

            Assert.Contains(questionBank.Questions, question => question.CorrectAnswer == revealing!.CorrectAnswer);
        }

        [Fact]
        public void Advance_FromRevealing_TransitionsToScoreboard()
        {
            var feederConfiguration = new QuizFeederConfiguration { QuestionDuration = TimeSpan.FromSeconds(1) };
            var loop = CreateLoop(out var session, out _, feederConfiguration);
            loop.Advance(); // Lobby -> Question
            loop.Advance(); // Question -> Revealing

            var scoreboard = loop.Advance(); // Revealing -> Scoreboard

            Assert.Equal(QuizPhase.Scoreboard, scoreboard!.Phase);
            Assert.Equal(QuizPhase.Scoreboard, session.PhaseStateMachine.CurrentPhase);
        }

        [Fact]
        public void Advance_FromScoreboard_MovesToTheNextQuestionAndResetsTimeRemaining()
        {
            // A 1-second QuestionDuration means exactly one tick exhausts it, keeping this test to one
            // Advance() call per phase rather than looping out an arbitrary countdown.
            var feederConfiguration = new QuizFeederConfiguration { QuestionDuration = TimeSpan.FromSeconds(1) };
            var loop = CreateLoop(out _, out _, feederConfiguration);
            loop.Advance(); // Lobby -> Question (index 0), 1s remaining
            loop.Advance(); // Question -> Revealing (the single 1s tick exhausts it)
            loop.Advance(); // Revealing -> Scoreboard

            var nextQuestion = loop.Advance(); // Scoreboard -> Question (index 1)

            Assert.Equal(QuizPhase.Question, nextQuestion!.Phase);
            Assert.Equal(1, nextQuestion.QuestionIndex);
            Assert.Equal(1, nextQuestion.TimeRemaining);
        }

        [Fact]
        public void FullGame_AdvancesThroughEveryQuestionAndReachesGameOverAutomatically()
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
                if (++guard > 10_000)
                    throw new TimeoutException("Game never reached GameOver.");
            }

            Assert.Equal(QuizPhase.GameOver, last!.Phase);
            Assert.Equal(questionBank.Count - 1, last.QuestionIndex);
            Assert.Equal(questionBank.Count, last.TotalQuestions);
        }

        [Fact]
        public void Advance_OnceGameOverIsReached_ReturnsNullAndNeverAdvancesAgain()
        {
            var feederConfiguration = new QuizFeederConfiguration
            {
                LobbyDuration = TimeSpan.FromMilliseconds(1),
                QuestionDuration = TimeSpan.FromMilliseconds(1),
                RevealingDuration = TimeSpan.FromMilliseconds(1),
                ScoreboardDuration = TimeSpan.FromMilliseconds(1)
            };
            var loop = CreateLoop(out var session, out _, feederConfiguration);

            var guard = 0;
            while (session.PhaseStateMachine.CurrentPhase != QuizPhase.GameOver)
            {
                loop.Advance();
                if (++guard > 10_000)
                    throw new TimeoutException("Game never reached GameOver.");
            }

            Assert.Null(loop.Advance());
            Assert.Null(loop.Advance());
            Assert.Equal(QuizPhase.GameOver, session.PhaseStateMachine.CurrentPhase);
        }

        // Issue #195's own AC: "Expose configurable ... question count" — QuizFeederConfiguration.QuestionsPerGame.
        [Fact]
        public void FullGame_WithAConfiguredQuestionsPerGame_PlaysExactlyThatManyQuestions()
        {
            var feederConfiguration = new QuizFeederConfiguration
            {
                LobbyDuration = TimeSpan.FromMilliseconds(1),
                QuestionDuration = TimeSpan.FromMilliseconds(1),
                RevealingDuration = TimeSpan.FromMilliseconds(1),
                ScoreboardDuration = TimeSpan.FromMilliseconds(1),
                QuestionsPerGame = 3
            };
            var loop = CreateLoop(out var session, out _, feederConfiguration);

            QuizChannelFeederMessage? last = null;
            var guard = 0;
            while (session.PhaseStateMachine.CurrentPhase != QuizPhase.GameOver)
            {
                last = loop.Advance();
                if (++guard > 10_000)
                    throw new TimeoutException("Game never reached GameOver.");
            }

            Assert.Equal(2, last!.QuestionIndex);
            Assert.Equal(3, last.TotalQuestions);
        }

        [Fact]
        public void FullGame_WithQuestionsPerGameLargerThanTheBank_PlaysTheWholeBankInsteadOfThrowing()
        {
            var feederConfiguration = new QuizFeederConfiguration
            {
                LobbyDuration = TimeSpan.FromMilliseconds(1),
                QuestionDuration = TimeSpan.FromMilliseconds(1),
                RevealingDuration = TimeSpan.FromMilliseconds(1),
                ScoreboardDuration = TimeSpan.FromMilliseconds(1),
                QuestionsPerGame = 1_000
            };
            var loop = CreateLoop(out var session, out var questionBank, feederConfiguration);

            QuizChannelFeederMessage? last = null;
            var guard = 0;
            while (session.PhaseStateMachine.CurrentPhase != QuizPhase.GameOver)
            {
                last = loop.Advance();
                if (++guard > 10_000)
                    throw new TimeoutException("Game never reached GameOver.");
            }

            Assert.Equal(questionBank.Count - 1, last!.QuestionIndex);
            Assert.Equal(questionBank.Count, last.TotalQuestions);
        }

        [Fact]
        public void NextDelay_OnceGameOverIsReached_IsStillPositive()
        {
            var feederConfiguration = new QuizFeederConfiguration
            {
                LobbyDuration = TimeSpan.FromMilliseconds(1),
                QuestionDuration = TimeSpan.FromMilliseconds(1),
                RevealingDuration = TimeSpan.FromMilliseconds(1),
                ScoreboardDuration = TimeSpan.FromMilliseconds(1)
            };
            var loop = CreateLoop(out var session, out _, feederConfiguration);

            var guard = 0;
            while (session.PhaseStateMachine.CurrentPhase != QuizPhase.GameOver)
            {
                loop.Advance();
                if (++guard > 10_000)
                    throw new TimeoutException("Game never reached GameOver.");
            }

            Assert.True(loop.NextDelay > TimeSpan.Zero);
        }
    }
}
