using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Game
{
    /// <summary>Issue #192: covers QuizGameLoopRegistry — the lookup QuizChannel.SubmitAnswer uses to reach the QuizGameLoop QuizFeeder actually drives.</summary>
    public sealed class QuizGameLoopRegistryTests
    {
        private static QuizGameLoop CreateLoop(string gameId) => new(new QuizGameSession(gameId), QuizQuestionBank.CreateDefault(), new QuizFeederConfiguration());

        [Fact]
        public void TryGet_BeforeAnyRegistration_ReturnsNull()
        {
            var registry = new QuizGameLoopRegistry();

            Assert.Null(registry.TryGet("game-1"));
        }

        [Fact]
        public void TryGet_AfterRegister_ReturnsTheSameInstance()
        {
            var registry = new QuizGameLoopRegistry();
            var loop = CreateLoop("game-1");

            registry.Register("game-1", loop);

            Assert.Same(loop, registry.TryGet("game-1"));
        }

        [Fact]
        public void Register_CalledAgainForTheSameGameId_ReplacesThePreviousLoop()
        {
            var registry = new QuizGameLoopRegistry();
            var first = CreateLoop("game-1");
            var second = CreateLoop("game-1");

            registry.Register("game-1", first);
            registry.Register("game-1", second);

            Assert.Same(second, registry.TryGet("game-1"));
        }

        [Fact]
        public void TryGet_ForADifferentGameId_ReturnsNull()
        {
            var registry = new QuizGameLoopRegistry();
            registry.Register("game-1", CreateLoop("game-1"));

            Assert.Null(registry.TryGet("game-2"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Register_WithNullOrWhiteSpaceGameId_Throws(string? gameId)
        {
            var registry = new QuizGameLoopRegistry();

            Assert.ThrowsAny<ArgumentException>(() => registry.Register(gameId!, CreateLoop("game-1")));
        }

        [Fact]
        public void Register_WithNullLoop_Throws()
        {
            var registry = new QuizGameLoopRegistry();

            Assert.Throws<ArgumentNullException>(() => registry.Register("game-1", null!));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void TryGet_WithNullOrWhiteSpaceGameId_Throws(string? gameId)
        {
            var registry = new QuizGameLoopRegistry();

            Assert.ThrowsAny<ArgumentException>(() => registry.TryGet(gameId!));
        }
    }
}
