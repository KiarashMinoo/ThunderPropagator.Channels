using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;
using ThunderPropagator.Providers.DotNet.Quiz;

namespace ThunderPropagator.Channels.Demo.Quiz.UnitTests
{
    public sealed class QuizProviderMessageTests
    {
        [Fact]
        public void Properties_RoundTripTheAssignedValues()
        {
            var scoreboard = new[] { new QuizScoreboardEntry("Alice", 100) };

            var message = new QuizProviderMessage
            {
                GameId = "game-1",
                Phase = QuizPhase.Question,
                QuestionText = "2 + 2?",
                Options = ["3", "4"],
                TimeRemaining = 5,
                QuestionIndex = 1,
                TotalQuestions = 20,
                Scoreboard = scoreboard,
                CorrectAnswer = "4",
                Winner = "Alice"
            };

            Assert.Equal("game-1", message.GameId);
            Assert.Equal(QuizPhase.Question, message.Phase);
            Assert.Equal("2 + 2?", message.QuestionText);
            Assert.Equal(["3", "4"], message.Options);
            Assert.Equal(5, message.TimeRemaining);
            Assert.Equal(1, message.QuestionIndex);
            Assert.Equal(20, message.TotalQuestions);
            Assert.Same(scoreboard, message.Scoreboard);
            Assert.Equal("4", message.CorrectAnswer);
            Assert.Equal("Alice", message.Winner);
        }

        [Fact]
        public void Defaults_AreEmptyRatherThanNull()
        {
            var message = new QuizProviderMessage { GameId = "game-1", Phase = QuizPhase.Lobby };

            Assert.Equal(string.Empty, message.QuestionText);
            Assert.Empty(message.Options);
            Assert.Equal(0, message.TimeRemaining);
            Assert.Empty(message.Scoreboard);
            Assert.Equal(string.Empty, message.CorrectAnswer);
            Assert.Equal(string.Empty, message.Winner);
        }
    }

    public sealed class QuizProviderConfigurationTests
    {
        [Fact]
        public void IsEnabled_DefaultsToTrue()
        {
            Assert.True(new QuizProviderConfiguration().IsEnabled);
        }
    }
}
