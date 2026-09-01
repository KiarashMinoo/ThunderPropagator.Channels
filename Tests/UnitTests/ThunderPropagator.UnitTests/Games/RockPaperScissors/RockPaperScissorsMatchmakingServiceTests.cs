using ThunderPropagator.Channels.Games.RockPaperScissors;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.UnitTests.Games.RockPaperScissors
{
    /// <summary>
    /// Issue #288: contract coverage for <see cref="RockPaperScissorsMatchmakingService"/> — the
    /// persisted replacement for RockPaperScissorsChannel's old node-local
    /// _sessions/_matchedConnectionIds dictionaries. The concurrency test below is this ticket's own
    /// central claim: a database-backed reservation gives the exact same "exactly one caller wins"
    /// guarantee <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}.TryAdd"/>
    /// gave the original in-memory implementation, now durable and visible cluster-wide instead of
    /// node-local.
    /// </summary>
    public sealed class RockPaperScissorsMatchmakingServiceTests
    {
        private sealed class FakeRockPaperScissorsContext : IRockPaperScissorsContext
        {
            private readonly List<RockPaperScissorsGameSessionRecord> _sessions = [];
            private readonly HashSet<string> _reservations = [];

            public Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
            {
                IReadOnlyCollection<TEntity> results = _sessions.OfType<TEntity>().ToList();
                return Task.FromResult(results);
            }

            public Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
            {
                _sessions.Add((RockPaperScissorsGameSessionRecord)(object)entity!);
                return Task.FromResult(entity);
            }

            // Deliberately not atomic in the fake — a plain lock around a HashSet.Add mirrors what a
            // check-then-insert provider would do, so the concurrency test below is actually
            // exercising TryReserveConnectionAsync's contract, not just re-proving HashSet.Add works.
            public Task<bool> TryReserveConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
            {
                lock (_reservations)
                    return Task.FromResult(_reservations.Add(connectionId));
            }
        }

        private static RockPaperScissorsMatchmakingService CreateService(out FakeRockPaperScissorsContext context)
        {
            context = new FakeRockPaperScissorsContext();
            return new RockPaperScissorsMatchmakingService(context);
        }

        [Fact]
        public async Task TryReserveConnectionAsync_FirstCall_ReturnsTrue()
        {
            var service = CreateService(out _);

            Assert.True(await service.TryReserveConnectionAsync("connection-1"));
        }

        [Fact]
        public async Task TryReserveConnectionAsync_CalledTwiceForTheSameConnection_TheSecondCallReturnsFalse()
        {
            var service = CreateService(out _);
            await service.TryReserveConnectionAsync("connection-1");

            Assert.False(await service.TryReserveConnectionAsync("connection-1"));
        }

        [Fact]
        public async Task TryReserveConnectionAsync_ForDifferentConnections_BothReturnTrue()
        {
            var service = CreateService(out _);

            Assert.True(await service.TryReserveConnectionAsync("connection-1"));
            Assert.True(await service.TryReserveConnectionAsync("connection-2"));
        }

        // Issue #21's original in-memory fix, re-proven against the persisted replacement: many
        // concurrent callers racing for the same connectionId must produce exactly one winner.
        // Dedicated threads rather than pooled Tasks: ThreadPool's throttled thread-injection rate can
        // take several seconds to ramp up to callerCount workers, which would make this test slow
        // without adding anything to what it proves.
        [Fact]
        public void TryReserveConnectionAsync_ManyConcurrentCallersForTheSameConnection_ExactlyOneWins()
        {
            const int callerCount = 32;
            var service = CreateService(out _);
            using var barrier = new Barrier(callerCount);
            var results = new bool[callerCount];

            var threads = Enumerable.Range(0, callerCount)
                .Select(i => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    results[i] = service.TryReserveConnectionAsync("connection-1").GetAwaiter().GetResult();
                }))
                .ToArray();

            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                thread.Join();

            Assert.Equal(1, results.Count(result => result));
        }

        [Fact]
        public async Task RecordSessionAsync_PersistsARetrievableSession()
        {
            var service = CreateService(out _);
            var firstPlayer = new Player("Alice", PlayerType.Human, MoveKind.Rock);
            var secondPlayer = new Player("Computer", PlayerType.Computer, MoveKind.Scissor);

            await service.RecordSessionAsync(firstPlayer, secondPlayer);

            var session = Assert.Single(await service.GetSessionsAsync());
            Assert.Equal("Alice", session.FirstPlayerName);
            Assert.Equal(MoveKind.Rock, session.FirstPlayerMove);
            Assert.Equal("Computer", session.SecondPlayerName);
            Assert.Equal(PlayerType.Computer, session.SecondPlayerType);
        }

        [Fact]
        public async Task GetSessionsAsync_WithNoRecordedSessions_ReturnsEmpty()
        {
            var service = CreateService(out _);

            Assert.Empty(await service.GetSessionsAsync());
        }
    }
}
