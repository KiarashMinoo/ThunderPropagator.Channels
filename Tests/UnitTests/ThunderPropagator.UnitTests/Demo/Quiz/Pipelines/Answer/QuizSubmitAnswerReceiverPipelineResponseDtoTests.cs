using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;
using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Answer;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Pipelines.Answer
{
    public sealed class QuizSubmitAnswerReceiverPipelineResponseDtoTests
    {
        [Theory]
        [InlineData(QuizAnswerOutcome.Correct)]
        [InlineData(QuizAnswerOutcome.Incorrect)]
        [InlineData(QuizAnswerOutcome.WindowClosed)]
        [InlineData(QuizAnswerOutcome.Duplicate)]
        [InlineData(QuizAnswerOutcome.Stale)]
        [InlineData(QuizAnswerOutcome.Invalid)]
        public void Outcome_Getter_ReturnsTheValueAssigned(QuizAnswerOutcome outcome)
        {
            var dto = new QuizSubmitAnswerReceiverPipelineResponseDto { Outcome = outcome };

            Assert.Equal(outcome, dto.Outcome);
        }

        [Fact]
        public void ResponseDto_ExposesOnlyOutcome()
        {
            // An exhaustive allow-list: #192's own AC ("Correct-answer data is not leaked in the
            // acknowledgement") means no future field should be added here without a deliberate
            // decision that it, too, is safe to reveal before the reveal phase.
            var type = typeof(QuizSubmitAnswerReceiverPipelineResponseDto);

            var propertyNames = type.GetProperties()
                .Where(property => property.DeclaringType == type)
                .Select(property => property.Name)
                .ToArray();

            Assert.Equal([nameof(QuizSubmitAnswerReceiverPipelineResponseDto.Outcome)], propertyNames);
        }
    }
}
