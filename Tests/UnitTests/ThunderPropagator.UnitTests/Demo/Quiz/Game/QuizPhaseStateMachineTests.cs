using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;
using ThunderPropagator.Channels.Demo.Quiz.Game.Exceptions;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Game
{
    /// <summary>
    /// Issue #184: covers QuizPhaseStateMachine's enforced lifecycle — the normal
    /// Lobby → Question → Revealing → Scoreboard → next Question or GameOver sequence, every invalid
    /// transition being rejected without mutating the current phase, and concurrent callers racing
    /// the same transition never both succeeding.
    /// </summary>
    public sealed class QuizPhaseStateMachineTests
    {
        [Fact]
        public void CurrentPhase_WhenNewlyCreated_IsLobby()
        {
            var stateMachine = new QuizPhaseStateMachine();

            Assert.Equal(QuizPhase.Lobby, stateMachine.CurrentPhase);
        }

        [Fact]
        public void StartGame_FromLobby_TransitionsToQuestion()
        {
            var stateMachine = new QuizPhaseStateMachine();

            var result = stateMachine.StartGame();

            Assert.Equal(QuizPhase.Question, result);
            Assert.Equal(QuizPhase.Question, stateMachine.CurrentPhase);
        }

        [Fact]
        public void StartGame_WhenNotInLobby_ThrowsAndLeavesPhaseUnchanged()
        {
            var stateMachine = new QuizPhaseStateMachine();
            stateMachine.StartGame();

            var exception = Assert.Throws<InvalidQuizPhaseTransitionException>(() => stateMachine.StartGame());

            Assert.Equal(QuizPhase.Question, exception.CurrentPhase);
            Assert.Equal(QuizPhase.Question, stateMachine.CurrentPhase);
        }

        [Fact]
        public void RevealAnswer_FromQuestion_TransitionsToRevealing()
        {
            var stateMachine = new QuizPhaseStateMachine();
            stateMachine.StartGame();

            var result = stateMachine.RevealAnswer();

            Assert.Equal(QuizPhase.Revealing, result);
            Assert.Equal(QuizPhase.Revealing, stateMachine.CurrentPhase);
        }

        [Fact]
        public void RevealAnswer_WhenNotInQuestion_ThrowsAndLeavesPhaseUnchanged()
        {
            var stateMachine = new QuizPhaseStateMachine();

            var exception = Assert.Throws<InvalidQuizPhaseTransitionException>(() => stateMachine.RevealAnswer());

            Assert.Equal(QuizPhase.Lobby, exception.CurrentPhase);
            Assert.Equal(QuizPhase.Lobby, stateMachine.CurrentPhase);
        }

        [Fact]
        public void ShowScoreboard_FromRevealing_TransitionsToScoreboard()
        {
            var stateMachine = new QuizPhaseStateMachine();
            stateMachine.StartGame();
            stateMachine.RevealAnswer();

            var result = stateMachine.ShowScoreboard();

            Assert.Equal(QuizPhase.Scoreboard, result);
            Assert.Equal(QuizPhase.Scoreboard, stateMachine.CurrentPhase);
        }

        [Fact]
        public void ShowScoreboard_WhenNotInRevealing_ThrowsAndLeavesPhaseUnchanged()
        {
            var stateMachine = new QuizPhaseStateMachine();

            var exception = Assert.Throws<InvalidQuizPhaseTransitionException>(() => stateMachine.ShowScoreboard());

            Assert.Equal(QuizPhase.Lobby, exception.CurrentPhase);
            Assert.Equal(QuizPhase.Lobby, stateMachine.CurrentPhase);
        }

        [Fact]
        public void NextQuestion_FromScoreboard_WithMoreQuestions_TransitionsToQuestion()
        {
            var stateMachine = new QuizPhaseStateMachine();
            stateMachine.StartGame();
            stateMachine.RevealAnswer();
            stateMachine.ShowScoreboard();

            var result = stateMachine.NextQuestion(hasMoreQuestions: true);

            Assert.Equal(QuizPhase.Question, result);
            Assert.Equal(QuizPhase.Question, stateMachine.CurrentPhase);
        }

        [Fact]
        public void NextQuestion_FromScoreboard_WithNoMoreQuestions_TransitionsToGameOver()
        {
            var stateMachine = new QuizPhaseStateMachine();
            stateMachine.StartGame();
            stateMachine.RevealAnswer();
            stateMachine.ShowScoreboard();

            var result = stateMachine.NextQuestion(hasMoreQuestions: false);

            Assert.Equal(QuizPhase.GameOver, result);
            Assert.Equal(QuizPhase.GameOver, stateMachine.CurrentPhase);
        }

        [Fact]
        public void NextQuestion_WhenNotInScoreboard_ThrowsAndLeavesPhaseUnchanged()
        {
            var stateMachine = new QuizPhaseStateMachine();

            var exception = Assert.Throws<InvalidQuizPhaseTransitionException>(() => stateMachine.NextQuestion(true));

            Assert.Equal(QuizPhase.Lobby, exception.CurrentPhase);
            Assert.Equal(QuizPhase.Lobby, stateMachine.CurrentPhase);
        }

        [Fact]
        public void Restart_FromGameOver_TransitionsToLobby()
        {
            var stateMachine = new QuizPhaseStateMachine();
            stateMachine.StartGame();
            stateMachine.RevealAnswer();
            stateMachine.ShowScoreboard();
            stateMachine.NextQuestion(hasMoreQuestions: false);

            var result = stateMachine.Restart();

            Assert.Equal(QuizPhase.Lobby, result);
            Assert.Equal(QuizPhase.Lobby, stateMachine.CurrentPhase);
        }

        [Theory]
        [InlineData(QuizPhase.Lobby)]
        [InlineData(QuizPhase.Question)]
        [InlineData(QuizPhase.Revealing)]
        [InlineData(QuizPhase.Scoreboard)]
        public void Restart_WhenNotInGameOver_ThrowsAndLeavesPhaseUnchanged(QuizPhase phase)
        {
            var stateMachine = MoveTo(phase);

            var exception = Assert.Throws<InvalidQuizPhaseTransitionException>(() => stateMachine.Restart());

            Assert.Equal(phase, exception.CurrentPhase);
            Assert.Equal(phase, stateMachine.CurrentPhase);
        }

        [Theory]
        [InlineData(QuizPhase.Question)]
        [InlineData(QuizPhase.Revealing)]
        [InlineData(QuizPhase.Scoreboard)]
        public void Cancel_FromAnyActivePhase_TransitionsToLobby(QuizPhase phase)
        {
            var stateMachine = MoveTo(phase);

            var result = stateMachine.Cancel();

            Assert.Equal(QuizPhase.Lobby, result);
            Assert.Equal(QuizPhase.Lobby, stateMachine.CurrentPhase);
        }

        [Fact]
        public void Cancel_FromLobby_ThrowsAndLeavesPhaseUnchanged()
        {
            var stateMachine = new QuizPhaseStateMachine();

            var exception = Assert.Throws<InvalidQuizPhaseTransitionException>(() => stateMachine.Cancel());

            Assert.Equal(QuizPhase.Lobby, exception.CurrentPhase);
            Assert.Equal(QuizPhase.Lobby, stateMachine.CurrentPhase);
        }

        [Fact]
        public void Cancel_FromGameOver_ThrowsAndLeavesPhaseUnchanged()
        {
            var stateMachine = MoveTo(QuizPhase.GameOver);

            var exception = Assert.Throws<InvalidQuizPhaseTransitionException>(() => stateMachine.Cancel());

            Assert.Equal(QuizPhase.GameOver, exception.CurrentPhase);
            Assert.Equal(QuizPhase.GameOver, stateMachine.CurrentPhase);
        }

        // Issue #184's own AC: "The normal sequence is Lobby → Question → Revealing → Scoreboard →
        // next Question or GameOver" — proven end to end here, including looping back for a second
        // question before finally ending the game, then a deliberate Restart back to Lobby.
        [Fact]
        public void FullLifecycle_FollowsTheDocumentedSequence()
        {
            var stateMachine = new QuizPhaseStateMachine();

            Assert.Equal(QuizPhase.Lobby, stateMachine.CurrentPhase);
            Assert.Equal(QuizPhase.Question, stateMachine.StartGame());
            Assert.Equal(QuizPhase.Revealing, stateMachine.RevealAnswer());
            Assert.Equal(QuizPhase.Scoreboard, stateMachine.ShowScoreboard());
            Assert.Equal(QuizPhase.Question, stateMachine.NextQuestion(hasMoreQuestions: true));
            Assert.Equal(QuizPhase.Revealing, stateMachine.RevealAnswer());
            Assert.Equal(QuizPhase.Scoreboard, stateMachine.ShowScoreboard());
            Assert.Equal(QuizPhase.GameOver, stateMachine.NextQuestion(hasMoreQuestions: false));
            Assert.Equal(QuizPhase.Lobby, stateMachine.Restart());
        }

        // Issue #184's own AC: "Concurrent start/timeout actions cannot advance a game twice" —
        // every thread races the exact same transition from the exact same starting phase; exactly
        // one may succeed, and the rest must observe the InvalidQuizPhaseTransitionException a
        // now-already-advanced phase produces, never a corrupted or double-advanced result.
        [Fact]
        public void RevealAnswer_CalledConcurrentlyFromMultipleThreads_ExactlyOneSucceeds()
        {
            const int threadCount = 16;
            var stateMachine = new QuizPhaseStateMachine();
            stateMachine.StartGame();

            var successCount = 0;
            var rejectionCount = 0;
            using var barrier = new Barrier(threadCount);

            var threads = Enumerable.Range(0, threadCount)
                .Select(_ => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    try
                    {
                        stateMachine.RevealAnswer();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (InvalidQuizPhaseTransitionException)
                    {
                        Interlocked.Increment(ref rejectionCount);
                    }
                }))
                .ToArray();

            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                thread.Join();

            Assert.Equal(1, successCount);
            Assert.Equal(threadCount - 1, rejectionCount);
            Assert.Equal(QuizPhase.Revealing, stateMachine.CurrentPhase);
        }

        private static QuizPhaseStateMachine MoveTo(QuizPhase phase)
        {
            var stateMachine = new QuizPhaseStateMachine();
            if (phase == QuizPhase.Lobby)
                return stateMachine;

            stateMachine.StartGame();
            if (phase == QuizPhase.Question)
                return stateMachine;

            stateMachine.RevealAnswer();
            if (phase == QuizPhase.Revealing)
                return stateMachine;

            stateMachine.ShowScoreboard();
            if (phase == QuizPhase.Scoreboard)
                return stateMachine;

            stateMachine.NextQuestion(hasMoreQuestions: false);
            return stateMachine;
        }
    }
}
