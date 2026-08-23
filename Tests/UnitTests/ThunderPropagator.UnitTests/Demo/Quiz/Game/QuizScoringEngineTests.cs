using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Game
{
    /// <summary>
    /// Issue #190: covers QuizScoringEngine in isolation from QuizGameLoop's timing — window
    /// enforcement (late/never-opened answers score nothing), duplicate rejection (a second submission
    /// can never increase a player's score, whether the first was right or wrong), the deterministic
    /// scoring formula (base points plus a response-time bonus scaled by how much of the question's
    /// countdown was left), scoreboard ordering/tie-breaking, and winner selection including ties.
    /// </summary>
    public sealed class QuizScoringEngineTests
    {
        private const string CorrectAnswer = "Paris";
        private static readonly TimeSpan QuestionDuration = TimeSpan.FromSeconds(10);

        [Fact]
        public void SubmitAnswer_BeforeAnyQuestionHasEverOpened_ReturnsWindowClosed()
        {
            var engine = new QuizScoringEngine();

            var outcome = engine.SubmitAnswer("Alice", CorrectAnswer, QuestionDuration);

            Assert.Equal(QuizAnswerOutcome.WindowClosed, outcome);
        }

        [Fact]
        public void SubmitAnswer_AfterCloseQuestion_ReturnsWindowClosed()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            engine.CloseQuestion();

            var outcome = engine.SubmitAnswer("Alice", CorrectAnswer, TimeSpan.Zero);

            Assert.Equal(QuizAnswerOutcome.WindowClosed, outcome);
        }

        [Fact]
        public void SubmitAnswer_AfterCloseQuestion_AwardsNoScore()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            engine.CloseQuestion();

            engine.SubmitAnswer("Alice", CorrectAnswer, QuestionDuration);

            Assert.Empty(engine.BuildScoreboard());
        }

        [Fact]
        public void SubmitAnswer_WhileOpenWithCorrectAnswer_ReturnsCorrect()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);

            var outcome = engine.SubmitAnswer("Alice", CorrectAnswer, QuestionDuration);

            Assert.Equal(QuizAnswerOutcome.Correct, outcome);
        }

        [Fact]
        public void SubmitAnswer_WhileOpenWithWrongAnswer_ReturnsIncorrectAndScoresNothing()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);

            var outcome = engine.SubmitAnswer("Alice", "London", QuestionDuration);

            Assert.Equal(QuizAnswerOutcome.Incorrect, outcome);
            Assert.Empty(engine.BuildScoreboard());
        }

        [Fact]
        public void SubmitAnswer_ASecondTimeForTheSameQuestion_ReturnsDuplicate()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            engine.SubmitAnswer("Alice", CorrectAnswer, QuestionDuration);

            var outcome = engine.SubmitAnswer("Alice", CorrectAnswer, QuestionDuration);

            Assert.Equal(QuizAnswerOutcome.Duplicate, outcome);
        }

        [Fact]
        public void SubmitAnswer_ASecondTimeAfterAWrongFirstAnswer_CannotCorrectItIntoAScore()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            engine.SubmitAnswer("Alice", "London", QuestionDuration);

            var outcome = engine.SubmitAnswer("Alice", CorrectAnswer, QuestionDuration);

            Assert.Equal(QuizAnswerOutcome.Duplicate, outcome);
            Assert.Empty(engine.BuildScoreboard());
        }

        [Fact]
        public void SubmitAnswer_ASecondTimeAfterACorrectFirstAnswer_DoesNotIncreaseScore()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            engine.SubmitAnswer("Alice", CorrectAnswer, QuestionDuration);
            var scoreAfterFirst = engine.BuildScoreboard().Single(entry => entry.PlayerName == "Alice").Score;

            engine.SubmitAnswer("Alice", CorrectAnswer, QuestionDuration);

            var scoreAfterSecond = engine.BuildScoreboard().Single(entry => entry.PlayerName == "Alice").Score;
            Assert.Equal(scoreAfterFirst, scoreAfterSecond);
        }

        [Fact]
        public void SubmitAnswer_CorrectWithFullTimeRemaining_AwardsBasePointsPlusFullBonus()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);

            engine.SubmitAnswer("Alice", CorrectAnswer, QuestionDuration);

            var score = engine.BuildScoreboard().Single().Score;
            Assert.Equal(QuizScoringEngine.CorrectAnswerBasePoints + QuizScoringEngine.MaxResponseTimeBonus, score);
        }

        [Fact]
        public void SubmitAnswer_CorrectWithNoTimeRemaining_AwardsOnlyBasePoints()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);

            engine.SubmitAnswer("Alice", CorrectAnswer, TimeSpan.Zero);

            var score = engine.BuildScoreboard().Single().Score;
            Assert.Equal(QuizScoringEngine.CorrectAnswerBasePoints, score);
        }

        [Fact]
        public void SubmitAnswer_CorrectWithHalfTimeRemaining_AwardsHalfTheBonus()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);

            engine.SubmitAnswer("Alice", CorrectAnswer, TimeSpan.FromSeconds(5));

            var score = engine.BuildScoreboard().Single().Score;
            Assert.Equal(QuizScoringEngine.CorrectAnswerBasePoints + QuizScoringEngine.MaxResponseTimeBonus / 2, score);
        }

        [Fact]
        public void SubmitAnswer_ScoresAccumulateAcrossQuestions()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            engine.SubmitAnswer("Alice", CorrectAnswer, TimeSpan.Zero); // +1000
            engine.CloseQuestion();

            engine.BeginQuestion("Tokyo", QuestionDuration);
            engine.SubmitAnswer("Alice", "Tokyo", TimeSpan.Zero); // +1000

            var score = engine.BuildScoreboard().Single().Score;
            Assert.Equal(QuizScoringEngine.CorrectAnswerBasePoints * 2, score);
        }

        [Fact]
        public void BeginQuestion_ResetsWhoHasAnsweredForTheNewQuestion()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            engine.SubmitAnswer("Alice", CorrectAnswer, QuestionDuration);
            engine.CloseQuestion();

            engine.BeginQuestion("Tokyo", QuestionDuration);
            var outcome = engine.SubmitAnswer("Alice", "Tokyo", QuestionDuration);

            Assert.Equal(QuizAnswerOutcome.Correct, outcome);
        }

        [Fact]
        public void BuildScoreboard_OrdersByScoreDescending()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            engine.SubmitAnswer("Alice", CorrectAnswer, TimeSpan.Zero); // 1000
            engine.SubmitAnswer("Bob", CorrectAnswer, QuestionDuration); // 1500

            var scoreboard = engine.BuildScoreboard();

            Assert.Equal(["Bob", "Alice"], scoreboard.Select(entry => entry.PlayerName));
        }

        [Fact]
        public void BuildScoreboard_BreaksTiesByPlayerNameOrdinalAscending()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            engine.SubmitAnswer("Zoe", CorrectAnswer, TimeSpan.Zero);
            engine.SubmitAnswer("Amy", CorrectAnswer, TimeSpan.Zero);

            var scoreboard = engine.BuildScoreboard();

            Assert.Equal(["Amy", "Zoe"], scoreboard.Select(entry => entry.PlayerName));
        }

        [Fact]
        public void BuildScoreboard_ExcludesPlayersWhoHaveNeverScored()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            engine.SubmitAnswer("Alice", "wrong answer", QuestionDuration);

            Assert.Empty(engine.BuildScoreboard());
        }

        [Fact]
        public void DetermineWinner_WithNoScores_ReturnsEmpty()
        {
            var engine = new QuizScoringEngine();

            Assert.Equal(string.Empty, engine.DetermineWinner());
        }

        [Fact]
        public void DetermineWinner_WithASingleTopScorer_ReturnsTheirName()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            engine.SubmitAnswer("Alice", CorrectAnswer, TimeSpan.Zero);
            engine.SubmitAnswer("Bob", "wrong", QuestionDuration);

            Assert.Equal("Alice", engine.DetermineWinner());
        }

        [Fact]
        public void DetermineWinner_WhenTied_ReturnsBothNamesOrdinalSorted()
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            engine.SubmitAnswer("Zoe", CorrectAnswer, TimeSpan.Zero);
            engine.SubmitAnswer("Amy", CorrectAnswer, TimeSpan.Zero);

            Assert.Equal("Amy, Zoe", engine.DetermineWinner());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void SubmitAnswer_WithNullOrWhiteSpacePlayerName_Throws(string? playerName)
        {
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);

            Assert.ThrowsAny<ArgumentException>(() => engine.SubmitAnswer(playerName!, CorrectAnswer, QuestionDuration));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void BeginQuestion_WithNullOrWhiteSpaceCorrectAnswer_Throws(string? correctAnswer)
        {
            var engine = new QuizScoringEngine();

            Assert.ThrowsAny<ArgumentException>(() => engine.BeginQuestion(correctAnswer!, QuestionDuration));
        }

        // Issue #190's own AC: "Scoring tests cover ... duplicates" under concurrency too — every
        // thread submits under a distinct player name, racing the same open question, and every single
        // one must be scored exactly once (no submission lost, none double-scored).
        [Fact]
        public void SubmitAnswer_CalledConcurrentlyByDistinctPlayers_ScoresEveryoneExactlyOnce()
        {
            const int playerCount = 32;
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            using var barrier = new Barrier(playerCount);

            var threads = Enumerable.Range(0, playerCount)
                .Select(index => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    engine.SubmitAnswer($"Player{index}", CorrectAnswer, QuestionDuration);
                }))
                .ToArray();

            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                thread.Join();

            var scoreboard = engine.BuildScoreboard();
            Assert.Equal(playerCount, scoreboard.Count);
            Assert.All(scoreboard, entry => Assert.Equal(QuizScoringEngine.CorrectAnswerBasePoints + QuizScoringEngine.MaxResponseTimeBonus, entry.Score));
        }

        [Fact]
        public void SubmitAnswer_CalledConcurrentlyByTheSamePlayer_ExactlyOneSucceeds()
        {
            const int threadCount = 16;
            var engine = new QuizScoringEngine();
            engine.BeginQuestion(CorrectAnswer, QuestionDuration);
            using var barrier = new Barrier(threadCount);

            var outcomes = new QuizAnswerOutcome[threadCount];
            var threads = Enumerable.Range(0, threadCount)
                .Select(index => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    outcomes[index] = engine.SubmitAnswer("Alice", CorrectAnswer, QuestionDuration);
                }))
                .ToArray();

            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                thread.Join();

            Assert.Equal(1, outcomes.Count(outcome => outcome == QuizAnswerOutcome.Correct));
            Assert.Equal(threadCount - 1, outcomes.Count(outcome => outcome == QuizAnswerOutcome.Duplicate));
            Assert.Equal(QuizScoringEngine.CorrectAnswerBasePoints + QuizScoringEngine.MaxResponseTimeBonus, engine.BuildScoreboard().Single().Score);
        }
    }
}
