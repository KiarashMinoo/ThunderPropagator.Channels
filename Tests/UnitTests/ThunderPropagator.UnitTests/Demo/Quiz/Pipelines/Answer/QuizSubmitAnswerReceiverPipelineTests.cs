using Microsoft.Extensions.Logging.Abstractions;
using ThunderPropagator.Channels.Demo.Quiz.Pipelines.Answer;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Pipelines.Answer
{
    /// <summary>Issue #192's own scope: "request key `Quiz/Answer`."</summary>
    public sealed class QuizSubmitAnswerReceiverPipelineTests
    {
        [Fact]
        public void RequestKey_IsQuizAnswer()
        {
            var pipeline = new QuizSubmitAnswerReceiverPipeline(NullLoggerFactory.Instance);

            Assert.Equal("Quiz/Answer", pipeline.RequestKey);
        }
    }
}
