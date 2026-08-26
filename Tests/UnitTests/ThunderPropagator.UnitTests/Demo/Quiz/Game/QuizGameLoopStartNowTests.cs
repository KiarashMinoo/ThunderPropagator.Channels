using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;
using ThunderPropagator.Channels.Demo.Quiz.Feeders;
using ThunderPropagator.Channels.Demo.Quiz.Messages;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Game
{
    /// <summary>
    /// Issue #193: covers QuizGameLoop.TryStartNow — the host-triggered early start, exercising exactly
    /// the same question-selection/scoring-setup path Advance()'s own Lobby case uses (already covered
    /// end-to-end by QuizGameLoopTests/QuizGameLoopAnswerIntegrationTests), plus the "only one caller
    /// ever actually starts it" guarantee #193's own AC asks for under concurrency.
    /// </summary>
    public sealed class QuizGameLoopStartNowTests
    {
        private const string GameId = "game-1";

        private static QuizGameLoop CreateLoop(out QuizGameSession session, out QuizQuestionBank questionBank)
        {
            session = new QuizGameSession(GameId);
            questionBank = QuizQuestionBank.CreateDefault();
            return new QuizGameLoop(session, questionBank, new QuizFeederConfiguration());
        }

        [Fact]
        public void TryStartNow_WhileInLobby_TransitionsToQuestion()
        {
            var loop = CreateLoop(out var session, out _);

            var message = loop.TryStartNow();

            Assert.NotNull(message);
            Assert.Equal(QuizPhase.Question, message.Phase);
            Assert.Equal(QuizPhase.Question, session.PhaseStateMachine.CurrentPhase);
        }

        [Fact]
        public void TryStartNow_WhileInLobby_PopulatesTheFirstQuestion()
        {
            var loop = CreateLoop(out _, out var questionBank);

            var message = loop.TryStartNow();

            Assert.Equal(0, message!.QuestionIndex);
            Assert.Equal(questionBank.Count, message.TotalQuestions);
            Assert.Contains(questionBank.Questions, question => question.Text == message.QuestionText);
        }

        [Fact]
        public void TryStartNow_WhileInLobby_RecordsTheReturnedMessageAsTheSessionsCurrentState()
        {
            var loop = CreateLoop(out var session, out _);

            var message = loop.TryStartNow();

            Assert.Same(message, session.CurrentState);
        }

        [Fact]
        public void TryStartNow_AfterAlreadyStarted_ReturnsNull()
        {
            var loop = CreateLoop(out _, out _);
            loop.TryStartNow();

            var second = loop.TryStartNow();

            Assert.Null(second);
        }

        [Fact]
        public void TryStartNow_AfterAlreadyStarted_DoesNotChangeTheActiveQuestion()
        {
            var loop = CreateLoop(out var session, out _);
            var first = loop.TryStartNow();

            loop.TryStartNow();

            Assert.Same(first, session.CurrentState);
        }

        [Fact]
        public void TryStartNow_OnceAdvancedPastLobbyViaAdvance_ReturnsNull()
        {
            var loop = CreateLoop(out _, out _);
            loop.Advance(); // Lobby -> Question, via the autonomous path

            Assert.Null(loop.TryStartNow());
        }

        // Issue #193's own AC: "Concurrent requests create one running loop" — every thread races the
        // exact same TryStartNow call on the exact same session; exactly one may observe Lobby and
        // actually start it, the rest must observe it already started (null), never a corrupted or
        // double-started result.
        [Fact]
        public void TryStartNow_CalledConcurrently_ExactlyOneSucceeds()
        {
            const int threadCount = 16;
            var loop = CreateLoop(out var session, out _);
            using var barrier = new Barrier(threadCount);

            var results = new QuizChannelFeederMessage?[threadCount];
            var threads = Enumerable.Range(0, threadCount)
                .Select(index => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    results[index] = loop.TryStartNow();
                }))
                .ToArray();

            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                thread.Join();

            Assert.Equal(1, results.Count(result => result is not null));
            Assert.Equal(QuizPhase.Question, session.PhaseStateMachine.CurrentPhase);
        }
    }
}
