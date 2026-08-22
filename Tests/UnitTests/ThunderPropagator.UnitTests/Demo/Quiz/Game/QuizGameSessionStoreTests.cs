using ThunderPropagator.Channels.Demo.Quiz.Game;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Game
{
    /// <summary>
    /// Issue #187: covers QuizGameSessionStore's registry behavior — GetOrCreateSession's
    /// once-per-GameId guarantee, that two different GameIds are never the same session instance (the
    /// AC's "two concurrent games cannot affect each other"), and RemoveIfAbandoned only ever releasing
    /// a session with no connected player.
    /// </summary>
    public sealed class QuizGameSessionStoreTests
    {
        [Fact]
        public void GetOrCreateSession_CalledTwiceForSameGameId_ReturnsTheSameInstance()
        {
            var store = new QuizGameSessionStore();

            var first = store.GetOrCreateSession("game-1");
            var second = store.GetOrCreateSession("game-1");

            Assert.Same(first, second);
        }

        [Fact]
        public void GetOrCreateSession_ForDifferentGameIds_ReturnsIsolatedInstances()
        {
            var store = new QuizGameSessionStore();

            var gameOne = store.GetOrCreateSession("game-1");
            var gameTwo = store.GetOrCreateSession("game-2");

            Assert.NotSame(gameOne, gameTwo);

            gameOne.Join("Alice", "conn-1");

            Assert.Single(gameOne.Players);
            Assert.Empty(gameTwo.Players);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GetOrCreateSession_WithNullOrWhiteSpaceGameId_Throws(string? gameId)
        {
            var store = new QuizGameSessionStore();

            Assert.ThrowsAny<ArgumentException>(() => store.GetOrCreateSession(gameId!));
        }

        [Fact]
        public void TryGetSession_ForUnknownGameId_ReturnsNull()
        {
            var store = new QuizGameSessionStore();

            Assert.Null(store.TryGetSession("does-not-exist"));
        }

        [Fact]
        public void TryGetSession_ForKnownGameId_ReturnsTheSameInstanceGetOrCreateSessionReturned()
        {
            var store = new QuizGameSessionStore();
            var created = store.GetOrCreateSession("game-1");

            var found = store.TryGetSession("game-1");

            Assert.Same(created, found);
        }

        [Fact]
        public void RemoveIfAbandoned_ForUnknownGameId_ReturnsFalse()
        {
            var store = new QuizGameSessionStore();

            Assert.False(store.RemoveIfAbandoned("does-not-exist"));
        }

        [Fact]
        public void RemoveIfAbandoned_WithAConnectedPlayer_ReturnsFalseAndKeepsSession()
        {
            var store = new QuizGameSessionStore();
            var session = store.GetOrCreateSession("game-1");
            session.Join("Alice", "conn-1");

            var removed = store.RemoveIfAbandoned("game-1");

            Assert.False(removed);
            Assert.Same(session, store.TryGetSession("game-1"));
        }

        [Fact]
        public void RemoveIfAbandoned_AfterEveryPlayerDisconnects_ReturnsTrueAndReleasesTheSession()
        {
            var store = new QuizGameSessionStore();
            var session = store.GetOrCreateSession("game-1");
            session.Join("Alice", "conn-1");
            session.Disconnect("conn-1");

            var removed = store.RemoveIfAbandoned("game-1");

            Assert.True(removed);
            Assert.Null(store.TryGetSession("game-1"));
        }

        [Fact]
        public void RemoveIfAbandoned_ThenGetOrCreateSession_ProducesAFreshSession()
        {
            var store = new QuizGameSessionStore();
            var original = store.GetOrCreateSession("game-1");
            original.Join("Alice", "conn-1");
            original.Disconnect("conn-1");
            store.RemoveIfAbandoned("game-1");

            var recreated = store.GetOrCreateSession("game-1");

            Assert.NotSame(original, recreated);
            Assert.Empty(recreated.Players);
        }

        // Issue #187's own AC: "Two concurrent games cannot affect each other" — every thread
        // exclusively touches its own GameId's session (create, join, disconnect, remove), so the
        // store's shared ConcurrentDictionary must never let one thread's game influence another's.
        [Fact]
        public void ConcurrentOperations_AcrossManyDistinctGameIds_NeverCrossContaminate()
        {
            const int gameCount = 32;
            var store = new QuizGameSessionStore();
            using var barrier = new Barrier(gameCount);

            var threads = Enumerable.Range(0, gameCount)
                .Select(index => new Thread(() =>
                {
                    var gameId = $"game-{index}";
                    barrier.SignalAndWait();

                    var session = store.GetOrCreateSession(gameId);
                    session.Join($"Player{index}", $"conn-{index}");

                    Assert.Equal(gameId, session.GameId);
                    Assert.Single(session.Players);
                    Assert.Equal($"Player{index}", session.Players[0].PlayerName);
                }))
                .ToArray();

            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                thread.Join();

            Assert.Equal(gameCount, store.SessionCount);
        }
    }
}
