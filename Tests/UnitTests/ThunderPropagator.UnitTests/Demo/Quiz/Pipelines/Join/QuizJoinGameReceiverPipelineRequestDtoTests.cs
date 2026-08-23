using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Join;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Pipelines.Join
{
    public sealed class QuizJoinGameReceiverPipelineRequestDtoTests
    {
        [Fact]
        public void GameId_Getter_ReturnsTheValueAssigned()
        {
            var dto = new QuizJoinGameReceiverPipelineRequestDto { GameId = "game-1", PlayerName = "Alice" };

            Assert.Equal("game-1", dto.GameId);
        }

        [Fact]
        public void PlayerName_Getter_ReturnsTheValueAssigned()
        {
            var dto = new QuizJoinGameReceiverPipelineRequestDto { GameId = "game-1", PlayerName = "Alice" };

            Assert.Equal("Alice", dto.PlayerName);
        }

        [Fact]
        public void GameId_IsStoredUnderItsOwnDictionaryKey()
        {
            // This is the level GetRequestContentFormData<T> actually binds through — the DTO is
            // itself a dictionary, so proving the key round-trips here is the direct equivalent of
            // proving a bound (deserialized) request does too.
            var dto = new QuizJoinGameReceiverPipelineRequestDto { GameId = "game-1", PlayerName = "Alice" };

            Assert.Equal("game-1", dto[nameof(QuizJoinGameReceiverPipelineRequestDto.GameId)]);
        }

        [Fact]
        public void PlayerName_IsStoredUnderItsOwnDictionaryKey()
        {
            var dto = new QuizJoinGameReceiverPipelineRequestDto { GameId = "game-1", PlayerName = "Alice" };

            Assert.Equal("Alice", dto[nameof(QuizJoinGameReceiverPipelineRequestDto.PlayerName)]);
        }
    }
}
