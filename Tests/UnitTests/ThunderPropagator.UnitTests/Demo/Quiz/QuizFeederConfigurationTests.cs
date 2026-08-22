using ThunderPropagator.Channels.Demo.Quiz;

namespace ThunderPropagator.UnitTests.Demo.Quiz
{
    /// <summary>
    /// Issue #189's own AC: "Default flow uses 10s lobby, 15s question, 3s reveal, and 5s scoreboard
    /// durations unless configured otherwise" — covers those defaults, that each duration accepts a
    /// configured override, and that a zero or negative duration is rejected immediately (a duration
    /// that never elapses is exactly the busy-spin #189's own AC forbids).
    /// </summary>
    public sealed class QuizFeederConfigurationTests
    {
        [Fact]
        public void Defaults_MatchTheDocumentedFlow()
        {
            var configuration = new QuizFeederConfiguration();

            Assert.Equal(TimeSpan.FromSeconds(10), configuration.LobbyDuration);
            Assert.Equal(TimeSpan.FromSeconds(15), configuration.QuestionDuration);
            Assert.Equal(TimeSpan.FromSeconds(3), configuration.RevealingDuration);
            Assert.Equal(TimeSpan.FromSeconds(5), configuration.ScoreboardDuration);
        }

        [Fact]
        public void Defaults_HaveIsEnabledTrue()
        {
            Assert.True(new QuizFeederConfiguration().IsEnabled);
        }

        [Theory]
        [InlineData(nameof(QuizFeederConfiguration.LobbyDuration))]
        [InlineData(nameof(QuizFeederConfiguration.QuestionDuration))]
        [InlineData(nameof(QuizFeederConfiguration.RevealingDuration))]
        [InlineData(nameof(QuizFeederConfiguration.ScoreboardDuration))]
        public void EachDuration_AcceptsAConfiguredOverride(string propertyName)
        {
            var configuration = new QuizFeederConfiguration();
            var value = TimeSpan.FromSeconds(42);

            typeof(QuizFeederConfiguration).GetProperty(propertyName)!.SetValue(configuration, value);

            Assert.Equal(value, typeof(QuizFeederConfiguration).GetProperty(propertyName)!.GetValue(configuration));
        }

        [Theory]
        [InlineData(nameof(QuizFeederConfiguration.LobbyDuration))]
        [InlineData(nameof(QuizFeederConfiguration.QuestionDuration))]
        [InlineData(nameof(QuizFeederConfiguration.RevealingDuration))]
        [InlineData(nameof(QuizFeederConfiguration.ScoreboardDuration))]
        public void EachDuration_RejectsZero(string propertyName)
        {
            var configuration = new QuizFeederConfiguration();
            var property = typeof(QuizFeederConfiguration).GetProperty(propertyName)!;

            var exception = Assert.Throws<TargetInvocationException>(() => property.SetValue(configuration, TimeSpan.Zero));

            Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException);
        }

        [Theory]
        [InlineData(nameof(QuizFeederConfiguration.LobbyDuration))]
        [InlineData(nameof(QuizFeederConfiguration.QuestionDuration))]
        [InlineData(nameof(QuizFeederConfiguration.RevealingDuration))]
        [InlineData(nameof(QuizFeederConfiguration.ScoreboardDuration))]
        public void EachDuration_RejectsNegativeValues(string propertyName)
        {
            var configuration = new QuizFeederConfiguration();
            var property = typeof(QuizFeederConfiguration).GetProperty(propertyName)!;

            var exception = Assert.Throws<TargetInvocationException>(() => property.SetValue(configuration, TimeSpan.FromSeconds(-1)));

            Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException);
        }
    }
}
