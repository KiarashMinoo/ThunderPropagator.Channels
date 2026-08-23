using Microsoft.Extensions.Logging.Abstractions;
using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Start;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Pipelines.Start
{
    /// <summary>Issue #193's own scope: "request key `Quiz/Start`."</summary>
    public sealed class QuizStartGameReceiverPipelineTests
    {
        [Fact]
        public void RequestKey_IsQuizStart()
        {
            var pipeline = new QuizStartGameReceiverPipeline(NullLoggerFactory.Instance);

            Assert.Equal("Quiz/Start", pipeline.RequestKey);
        }
    }
}
