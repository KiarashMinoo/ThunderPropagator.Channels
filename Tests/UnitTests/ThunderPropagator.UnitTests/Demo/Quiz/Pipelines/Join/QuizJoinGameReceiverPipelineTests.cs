using Microsoft.Extensions.Logging.Abstractions;
using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Join;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Pipelines.Join
{
    /// <summary>Issue #191's own scope: "request key `Quiz/Join`."</summary>
    public sealed class QuizJoinGameReceiverPipelineTests
    {
        [Fact]
        public void RequestKey_IsQuizJoin()
        {
            var pipeline = new QuizJoinGameReceiverPipeline(NullLoggerFactory.Instance);

            Assert.Equal("Quiz/Join", pipeline.RequestKey);
        }
    }
}
