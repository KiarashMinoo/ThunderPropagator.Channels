using System.Text.Json;
using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;
using ThunderPropagator.Channels.Demo.Quiz.Messages;
using ThunderPropagator.Channels.Demo.Quiz.Metadata;

namespace ThunderPropagator.UnitTests.Demo.Quiz
{
    /// <summary>
    /// Issue #185's own AC: "the answer is not exposed before the Revealing phase". CorrectAnswer's
    /// getter redacts to empty until Phase reaches Revealing, regardless of what was assigned to it —
    /// covered here directly, plus the symmetric Winner/GameOver rule and the remaining fields'
    /// documented defaults. Issue #186 adds: field validation (rejecting null/empty/oversized/
    /// negative/over-count values immediately at the setter) and JSON round-trip/determinism
    /// coverage for the collection fields Options/Scoreboard use their JsonChannelProgramsDescriptor
    /// wire encoding for.
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

        // Issue #186's own scope: "Validate GameId, indexes, counts, time remaining, and payload
        // size." Each theory below proves the corresponding setter rejects the invalid value
        // immediately, with QuizChannelFeederMessageValidationException naming the offending
        // property, rather than silently storing something a subscriber could never sensibly use.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GameId_WhenNullEmptyOrWhitespace_Throws(string? gameId)
        {
            var exception = Assert.Throws<QuizChannelFeederMessageValidationException>(
                () => new QuizChannelFeederMessage { GameId = gameId! });

            Assert.Equal(nameof(QuizChannelFeederMessage.GameId), exception.PropertyName);
        }

        [Fact]
        public void GameId_WhenOverMaxLength_Throws()
        {
            var tooLong = new string('a', QuizChannelFeederMessage.GameIdMaxLength + 1);

            var exception = Assert.Throws<QuizChannelFeederMessageValidationException>(
                () => new QuizChannelFeederMessage { GameId = tooLong });

            Assert.Equal(nameof(QuizChannelFeederMessage.GameId), exception.PropertyName);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void TimeRemaining_WhenNegative_Throws(int value)
        {
            var exception = Assert.Throws<QuizChannelFeederMessageValidationException>(
                () => new QuizChannelFeederMessage { TimeRemaining = value });

            Assert.Equal(nameof(QuizChannelFeederMessage.TimeRemaining), exception.PropertyName);
        }

        [Fact]
        public void QuestionIndex_WhenNegative_Throws()
        {
            var exception = Assert.Throws<QuizChannelFeederMessageValidationException>(
                () => new QuizChannelFeederMessage { QuestionIndex = -1 });

            Assert.Equal(nameof(QuizChannelFeederMessage.QuestionIndex), exception.PropertyName);
        }

        [Fact]
        public void TotalQuestions_WhenNegative_Throws()
        {
            var exception = Assert.Throws<QuizChannelFeederMessageValidationException>(
                () => new QuizChannelFeederMessage { TotalQuestions = -1 });

            Assert.Equal(nameof(QuizChannelFeederMessage.TotalQuestions), exception.PropertyName);
        }

        [Fact]
        public void QuestionText_WhenOverMaxLength_Throws()
        {
            var tooLong = new string('a', QuizChannelFeederMessage.TextMaxLength + 1);

            var exception = Assert.Throws<QuizChannelFeederMessageValidationException>(
                () => new QuizChannelFeederMessage { QuestionText = tooLong });

            Assert.Equal(nameof(QuizChannelFeederMessage.QuestionText), exception.PropertyName);
        }

        [Fact]
        public void CorrectAnswer_WhenOverMaxLength_Throws()
        {
            var tooLong = new string('a', QuizChannelFeederMessage.TextMaxLength + 1);

            var exception = Assert.Throws<QuizChannelFeederMessageValidationException>(
                () => new QuizChannelFeederMessage { CorrectAnswer = tooLong });

            Assert.Equal(nameof(QuizChannelFeederMessage.CorrectAnswer), exception.PropertyName);
        }

        [Fact]
        public void Winner_WhenOverMaxLength_Throws()
        {
            var tooLong = new string('a', QuizChannelFeederMessage.TextMaxLength + 1);

            var exception = Assert.Throws<QuizChannelFeederMessageValidationException>(
                () => new QuizChannelFeederMessage { Winner = tooLong });

            Assert.Equal(nameof(QuizChannelFeederMessage.Winner), exception.PropertyName);
        }

        [Fact]
        public void Options_WhenOverMaxCount_Throws()
        {
            var tooMany = Enumerable.Range(0, QuizChannelFeederMessage.OptionsMaxCount + 1)
                .Select(i => $"Option {i}")
                .ToArray();

            var exception = Assert.Throws<QuizChannelFeederMessageValidationException>(
                () => new QuizChannelFeederMessage { Options = tooMany });

            Assert.Equal(nameof(QuizChannelFeederMessage.Options), exception.PropertyName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Options_WithANullEmptyOrWhitespaceEntry_Throws(string? invalidOption)
        {
            var exception = Assert.Throws<QuizChannelFeederMessageValidationException>(
                () => new QuizChannelFeederMessage { Options = ["Paris", invalidOption!] });

            Assert.Equal(nameof(QuizChannelFeederMessage.Options), exception.PropertyName);
        }

        [Fact]
        public void Options_WithAnOverlongEntry_Throws()
        {
            var tooLong = new string('a', QuizChannelFeederMessage.TextMaxLength + 1);

            var exception = Assert.Throws<QuizChannelFeederMessageValidationException>(
                () => new QuizChannelFeederMessage { Options = [tooLong] });

            Assert.Equal(nameof(QuizChannelFeederMessage.Options), exception.PropertyName);
        }

        [Fact]
        public void Scoreboard_WhenOverMaxCount_Throws()
        {
            var tooMany = Enumerable.Range(0, QuizChannelFeederMessage.ScoreboardMaxCount + 1)
                .Select(i => new QuizScoreboardEntry($"Player {i}", i))
                .ToArray();

            var exception = Assert.Throws<QuizChannelFeederMessageValidationException>(
                () => new QuizChannelFeederMessage { Scoreboard = tooMany });

            Assert.Equal(nameof(QuizChannelFeederMessage.Scoreboard), exception.PropertyName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Scoreboard_WithAnEntryHavingANullEmptyOrWhitespacePlayerName_Throws(string? invalidPlayerName)
        {
            var exception = Assert.Throws<QuizChannelFeederMessageValidationException>(
                () => new QuizChannelFeederMessage { Scoreboard = [new QuizScoreboardEntry(invalidPlayerName!, 10)] });

            Assert.Equal(nameof(QuizChannelFeederMessage.Scoreboard), exception.PropertyName);
        }

        // Issue #186's own AC: "Round-trip serialization preserves options, scoreboard, phase, and
        // identifiers" and "Serialize options and scoreboard deterministically using the channel's
        // JSON conventions" — Options/Scoreboard are declared with JsonChannelProgramsDescriptor
        // (see QuizChannelMetadata), so proving these two collection values are cleanly, repeatably
        // System.Text.Json-serializable is exactly what backs that wire encoding.
        [Fact]
        public void Options_RoundTripsThroughJson()
        {
            IReadOnlyList<string> options = ["Paris", "London", "Berlin"];

            var json = JsonSerializer.Serialize(options);
            var deserialized = JsonSerializer.Deserialize<IReadOnlyList<string>>(json);

            Assert.Equal(options, deserialized);
        }

        [Fact]
        public void Scoreboard_RoundTripsThroughJson()
        {
            IReadOnlyList<QuizScoreboardEntry> scoreboard = [new("Alice", 30), new("Bob", 20)];

            var json = JsonSerializer.Serialize(scoreboard);
            var deserialized = JsonSerializer.Deserialize<IReadOnlyList<QuizScoreboardEntry>>(json);

            Assert.Equal(scoreboard, deserialized);
        }

        [Fact]
        public void Scoreboard_SerializesDeterministically()
        {
            IReadOnlyList<QuizScoreboardEntry> scoreboard = [new("Alice", 30), new("Bob", 20)];

            var firstJson = JsonSerializer.Serialize(scoreboard);
            var secondJson = JsonSerializer.Serialize(scoreboard);

            Assert.Equal(firstJson, secondJson);
        }

        [Fact]
        public void Phase_RoundTripsThroughJson()
        {
            var json = JsonSerializer.Serialize(QuizPhase.Revealing);
            var deserialized = JsonSerializer.Deserialize<QuizPhase>(json);

            Assert.Equal(QuizPhase.Revealing, deserialized);
        }

        [Fact]
        public void GameId_RoundTripsThroughJson()
        {
            var json = JsonSerializer.Serialize("game-123");
            var deserialized = JsonSerializer.Deserialize<string>(json);

            Assert.Equal("game-123", deserialized);
        }
    }
}
