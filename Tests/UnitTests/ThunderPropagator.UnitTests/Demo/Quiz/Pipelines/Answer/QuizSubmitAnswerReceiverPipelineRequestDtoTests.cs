using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Answer;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Pipelines.Answer
{
    public sealed class QuizSubmitAnswerReceiverPipelineRequestDtoTests
    {
        private static QuizSubmitAnswerReceiverPipelineRequestDto CreateDto() => new()
        {
            GameId = "game-1",
            QuestionIndex = 3,
            OptionIndex = 2
        };

        [Fact]
        public void GameId_Getter_ReturnsTheValueAssigned()
        {
            Assert.Equal("game-1", CreateDto().GameId);
        }

        [Fact]
        public void QuestionIndex_Getter_ReturnsTheValueAssigned()
        {
            Assert.Equal(3, CreateDto().QuestionIndex);
        }

        [Fact]
        public void OptionIndex_Getter_ReturnsTheValueAssigned()
        {
            Assert.Equal(2, CreateDto().OptionIndex);
        }

        [Fact]
        public void GameId_IsStoredUnderItsOwnDictionaryKey()
        {
            // This is the level GetRequestContentFormData<T> actually binds through — the DTO is
            // itself a dictionary, so proving the key round-trips here is the direct equivalent of
            // proving a bound (deserialized) request does too.
            Assert.Equal("game-1", CreateDto()[nameof(QuizSubmitAnswerReceiverPipelineRequestDto.GameId)]);
        }

        [Fact]
        public void QuestionIndex_IsStoredUnderItsOwnDictionaryKey()
        {
            Assert.Equal(3, CreateDto()[nameof(QuizSubmitAnswerReceiverPipelineRequestDto.QuestionIndex)]);
        }

        [Fact]
        public void OptionIndex_IsStoredUnderItsOwnDictionaryKey()
        {
            Assert.Equal(2, CreateDto()[nameof(QuizSubmitAnswerReceiverPipelineRequestDto.OptionIndex)]);
        }
    }
}
