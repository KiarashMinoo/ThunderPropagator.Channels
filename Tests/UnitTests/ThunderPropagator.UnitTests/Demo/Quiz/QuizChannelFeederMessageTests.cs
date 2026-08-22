using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.UnitTests.Demo.Quiz
{
    /// <summary>
    /// Issue #185's own AC: "the answer is not exposed before the Revealing phase". CorrectAnswer's
    /// getter redacts to empty until Phase reaches Revealing, regardless of what was assigned to it —
    /// covered here directly, plus the symmetric Winner/GameOver rule and the remaining fields'
    /// documented defaults.
    /// </summary>
    public sealed class QuizChannelFeederMessageTests
    {
        [Theory]
        [InlineData(QuizPhase.Lobby)]
        [InlineData(QuizPhase.Question)]
        public void CorrectAnswer_BeforeRevealing_ReadsAsEmptyRegardlessOfWhatWasAssigned(QuizPhase phase)
        {
            var message = new QuizChannelFeederMessage { Phase = phase, CorrectAnswer = "Paris" };

            Assert.Equal(string.Empty, message.CorrectAnswer);
        }

        [Theory]
        [InlineData(QuizPhase.Revealing)]
        [InlineData(QuizPhase.Scoreboard)]
        [InlineData(QuizPhase.GameOver)]
        public void CorrectAnswer_FromRevealingOnward_ReadsTheAssignedValue(QuizPhase phase)
        {
            var message = new QuizChannelFeederMessage { Phase = phase, CorrectAnswer = "Paris" };

            Assert.Equal("Paris", message.CorrectAnswer);
        }

        [Theory]
        [InlineData(QuizPhase.Lobby)]
        [InlineData(QuizPhase.Question)]
        [InlineData(QuizPhase.Revealing)]
        [InlineData(QuizPhase.Scoreboard)]
        public void Winner_BeforeGameOver_ReadsAsEmptyRegardlessOfWhatWasAssigned(QuizPhase phase)
        {
            var message = new QuizChannelFeederMessage { Phase = phase, Winner = "Alice" };

            Assert.Equal(string.Empty, message.Winner);
        }

        [Fact]
        public void Winner_AtGameOver_ReadsTheAssignedValue()
        {
            var message = new QuizChannelFeederMessage { Phase = QuizPhase.GameOver, Winner = "Alice" };

            Assert.Equal("Alice", message.Winner);
        }

        [Fact]
        public void Phase_WhenNeverSet_DefaultsToLobby()
        {
            var message = new QuizChannelFeederMessage();

            Assert.Equal(QuizPhase.Lobby, message.Phase);
        }

        [Fact]
        public void Options_WhenNeverSet_IsEmptyNotNull()
        {
            var message = new QuizChannelFeederMessage();

            Assert.Empty(message.Options);
        }

        [Fact]
        public void Scoreboard_WhenNeverSet_IsEmptyNotNull()
        {
            var message = new QuizChannelFeederMessage();

            Assert.Empty(message.Scoreboard);
        }

        [Fact]
        public void GameId_RoundTrips()
        {
            var message = new QuizChannelFeederMessage { GameId = "game-123" };

            Assert.Equal("game-123", message.GameId);
        }

        [Fact]
        public void QuestionTextAndOptions_RoundTrip()
        {
            var message = new QuizChannelFeederMessage
            {
                QuestionText = "What is the capital of France?",
                Options = ["Paris", "London", "Berlin"]
            };

            Assert.Equal("What is the capital of France?", message.QuestionText);
            Assert.Equal(["Paris", "London", "Berlin"], message.Options);
        }

        [Fact]
        public void TimingAndCountFields_RoundTrip()
        {
            var message = new QuizChannelFeederMessage
            {
                TimeRemaining = 15,
                QuestionIndex = 2,
                TotalQuestions = 10
            };

            Assert.Equal(15, message.TimeRemaining);
            Assert.Equal(2, message.QuestionIndex);
            Assert.Equal(10, message.TotalQuestions);
        }

        [Fact]
        public void Scoreboard_RoundTrips()
        {
            QuizScoreboardEntry[] scoreboard = [new("Alice", 30), new("Bob", 20)];

            var message = new QuizChannelFeederMessage { Scoreboard = scoreboard };

            Assert.Equal(scoreboard, message.Scoreboard);
        }
    }
}
