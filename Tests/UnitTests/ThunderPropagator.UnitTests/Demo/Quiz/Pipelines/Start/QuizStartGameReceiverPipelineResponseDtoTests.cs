using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;
using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Start;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Pipelines.Start
{
    public sealed class QuizStartGameReceiverPipelineResponseDtoTests
    {
        [Theory]
        [InlineData(QuizStartOutcome.Started)]
        [InlineData(QuizStartOutcome.AlreadyStarted)]
        public void Outcome_Getter_ReturnsTheValueAssigned(QuizStartOutcome outcome)
        {
            var dto = new QuizStartGameReceiverPipelineResponseDto { Outcome = outcome };

            Assert.Equal(outcome, dto.Outcome);
        }
    }
}
