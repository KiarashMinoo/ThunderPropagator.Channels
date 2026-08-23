using ThunderPropagator.Channels.Demo.Quiz;

namespace ThunderPropagator.UnitTests.Demo.Quiz
{
    /// <summary>
    /// Issue #195's own AC: "Expose configurable durations, maximum players, question count, and
    /// relevant validation rules" and "Defaults match the documented demo flow" — covers
    /// <see cref="QuizChannelConfiguration"/>'s own defaults and the per-property validation
    /// <see cref="QuizChannelConfiguration.MaxPlayers"/>/<see cref="QuizChannelConfiguration.MinPlayers"/>
    /// enforce independently. The cross-property rule between the two (MinPlayers must not exceed
    /// MaxPlayers) can only be checked once both are known, so it is covered by
    /// <c>QuizChannelExtensionsTests</c> instead, against <see cref="QuizChannelExtensions.AddQuizChannel"/>
    /// itself.
    /// </summary>
    public sealed class QuizChannelConfigurationTests
    {
        [Fact]
        public void Defaults_MatchTheDocumentedFlow()
        {
            var configuration = new QuizChannelConfiguration();

            Assert.True(configuration.IsEnabled);
            Assert.Equal(8, configuration.MaxPlayers);
            Assert.Equal(2, configuration.MinPlayers);
            Assert.True(configuration.AllowMidGameJoin);
        }

        [Fact]
        public void MaxPlayers_AcceptsAConfiguredOverride()
        {
            var configuration = new QuizChannelConfiguration { MaxPlayers = 20 };

            Assert.Equal(20, configuration.MaxPlayers);
        }

        [Fact]
        public void MaxPlayers_RejectsZero()
        {
            var configuration = new QuizChannelConfiguration();

            Assert.Throws<ArgumentOutOfRangeException>(() => configuration.MaxPlayers = 0);
        }

        [Fact]
        public void MaxPlayers_RejectsNegativeValues()
        {
            var configuration = new QuizChannelConfiguration();

            Assert.Throws<ArgumentOutOfRangeException>(() => configuration.MaxPlayers = -1);
        }

        [Fact]
        public void MinPlayers_AcceptsAConfiguredOverride()
        {
            var configuration = new QuizChannelConfiguration { MinPlayers = 4 };

            Assert.Equal(4, configuration.MinPlayers);
        }

        [Fact]
        public void MinPlayers_RejectsZero()
        {
            var configuration = new QuizChannelConfiguration();

            Assert.Throws<ArgumentOutOfRangeException>(() => configuration.MinPlayers = 0);
        }

        [Fact]
        public void MinPlayers_RejectsNegativeValues()
        {
            var configuration = new QuizChannelConfiguration();

            Assert.Throws<ArgumentOutOfRangeException>(() => configuration.MinPlayers = -1);
        }
    }
}
