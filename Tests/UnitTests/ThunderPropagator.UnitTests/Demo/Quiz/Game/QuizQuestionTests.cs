using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Exceptions;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Game
{
    /// <summary>
    /// Issue #188: covers QuizQuestion's own validation — text/options/correctOptionIndex are checked
    /// eagerly in the constructor, so a malformed question can never exist at all, matching the AC's
    /// "every question has a correct answer contained in its options" and "validation tests reject
    /// malformed questions".
    /// </summary>
    public sealed class QuizQuestionTests
    {
        private static readonly string[] ValidOptions = ["Paris", "London", "Berlin", "Madrid"];

        [Fact]
        public void Constructor_WithValidArguments_ExposesTheGivenValues()
        {
            var question = new QuizQuestion("What is the capital of France?", ValidOptions, 0);

            Assert.Equal("What is the capital of France?", question.Text);
            Assert.Equal(ValidOptions, question.Options);
            Assert.Equal(0, question.CorrectOptionIndex);
            Assert.Equal("Paris", question.CorrectAnswer);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidText_Throws(string? text)
        {
            var exception = Record.Exception(() => new QuizQuestion(text!, ValidOptions, 0));

            Assert.IsType<QuizQuestionValidationException>(exception);
        }

        [Fact]
        public void Constructor_WithNullOptions_Throws()
        {
            var exception = Record.Exception(() => new QuizQuestion("Question?", null!, 0));

            Assert.IsType<QuizQuestionValidationException>(exception);
        }

        [Fact]
        public void Constructor_WithFewerThanTheMinimumOptionCount_Throws()
        {
            var exception = Record.Exception(() => new QuizQuestion("Question?", ["OnlyOption"], 0));

            Assert.IsType<QuizQuestionValidationException>(exception);
        }

        [Fact]
        public void Constructor_WithExactlyTheMinimumOptionCount_Succeeds()
        {
            var exception = Record.Exception(() => new QuizQuestion("Question?", ["A", "B"], 0));

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithABlankOption_Throws(string? blankOption)
        {
            var exception = Record.Exception(() => new QuizQuestion("Question?", ["Paris", blankOption!, "Berlin"], 0));

            Assert.IsType<QuizQuestionValidationException>(exception);
        }

        [Fact]
        public void Constructor_WithDuplicateOptions_Throws()
        {
            var exception = Record.Exception(() => new QuizQuestion("Question?", ["Paris", "Paris", "Berlin"], 0));

            Assert.IsType<QuizQuestionValidationException>(exception);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public void Constructor_WithCorrectOptionIndexOutOfRange_Throws(int correctOptionIndex)
        {
            var exception = Record.Exception(() => new QuizQuestion("Question?", ValidOptions, correctOptionIndex));

            Assert.IsType<QuizQuestionValidationException>(exception);
        }

        [Fact]
        public void Constructor_WithCorrectOptionIndexAtTheLastValidIndex_Succeeds()
        {
            var question = new QuizQuestion("Question?", ValidOptions, ValidOptions.Length - 1);

            Assert.Equal("Madrid", question.CorrectAnswer);
        }
    }
}
