using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Start;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Pipelines.Start
{
    public sealed class QuizStartGameReceiverPipelineRequestDtoTests
    {
        [Fact]
        public void GameId_Getter_ReturnsTheValueAssigned()
        {
            var dto = new QuizStartGameReceiverPipelineRequestDto { GameId = "game-1" };

            Assert.Equal("game-1", dto.GameId);
        }

        [Fact]
        public void GameId_IsStoredUnderItsOwnDictionaryKey()
        {
            // This is the level GetRequestContentFormData<T> actually binds through — the DTO is
            // itself a dictionary, so proving the key round-trips here is the direct equivalent of
            // proving a bound (deserialized) request does too.
            var dto = new QuizStartGameReceiverPipelineRequestDto { GameId = "game-1" };

            Assert.Equal("game-1", dto[nameof(QuizStartGameReceiverPipelineRequestDto.GameId)]);
        }

        [Fact]
        public void RequestDto_ExposesOnlyGameId()
        {
            // #193's own scope: no player identity is ever supplied — the host is resolved server-side
            // from the calling connection. This is an exhaustive allow-list guarding against a future
            // field silently reintroducing a caller-supplied identity.
            var type = typeof(QuizStartGameReceiverPipelineRequestDto);

            var propertyNames = type.GetProperties()
                .Where(property => property.DeclaringType == type)
                .Select(property => property.Name)
                .ToArray();

            Assert.Equal([nameof(QuizStartGameReceiverPipelineRequestDto.GameId)], propertyNames);
        }
    }
}
