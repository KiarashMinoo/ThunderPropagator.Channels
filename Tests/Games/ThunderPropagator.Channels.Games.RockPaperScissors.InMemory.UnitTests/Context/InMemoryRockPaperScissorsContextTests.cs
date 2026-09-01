using ThunderPropagator.Channels.Games.RockPaperScissors.InMemory.Context;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.InMemory.UnitTests.Context
{
    /// <summary>
    /// Issue #288: contract coverage for <see cref="InMemoryRockPaperScissorsContext"/>/<see cref="InMemoryRockPaperScissorsStore"/>
    /// — mirrors ThunderPropagator.Channels.Chat.InMemory's own InMemoryChatContextTests.
    /// </summary>
    public sealed class InMemoryRockPaperScissorsContextTests
    {
        private static InMemoryRockPaperScissorsContext CreateContext() => new(new InMemoryRockPaperScissorsStore());

        [Fact]
        public async Task TryReserveConnectionAsync_FirstCall_ReturnsTrue()
        {
            var context = CreateContext();

            Assert.True(await context.TryReserveConnectionAsync("connection-1"));
        }

        [Fact]
        public async Task TryReserveConnectionAsync_CalledTwiceForTheSameConnection_TheSecondCallReturnsFalse()
        {
            var context = CreateContext();
            await context.TryReserveConnectionAsync("connection-1");

            Assert.False(await context.TryReserveConnectionAsync("connection-1"));
        }

        // Dedicated threads rather than pooled Tasks: ThreadPool's throttled thread-injection rate can
        // take several seconds to ramp up to callerCount workers, which would make this test slow
        // without adding anything to what it proves.
        [Fact]
        public void TryReserveConnectionAsync_ManyConcurrentCallersForTheSameConnection_ExactlyOneWins()
        {
            const int callerCount = 32;
            var context = CreateContext();
            using var barrier = new Barrier(callerCount);
            var results = new bool[callerCount];

            var threads = Enumerable.Range(0, callerCount)
                .Select(i => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    results[i] = context.TryReserveConnectionAsync("connection-1").GetAwaiter().GetResult();
                }))
                .ToArray();

            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                thread.Join();

            Assert.Equal(1, results.Count(result => result));
        }

        [Fact]
        public async Task CreateAsync_ThenGetAllAsync_ReturnsTheCreatedSession()
        {
            var context = CreateContext();
            var session = RockPaperScissorsGameSessionRecord.Create(
                new Player("Alice", PlayerType.Human, MoveKind.Rock),
                new Player("Computer", PlayerType.Computer, MoveKind.Scissor));

            await context.CreateAsync(session);

            var sessions = await context.GetAllAsync<RockPaperScissorsGameSessionRecord>();
            Assert.Equal(session.SessionId, Assert.Single(sessions).SessionId);
        }

        [Fact]
        public async Task GetAsync_ForAnUnknownId_ReturnsNull()
        {
            var context = CreateContext();

            Assert.Null(await context.GetAsync<RockPaperScissorsGameSessionRecord, string>("unknown-session"));
        }

        [Fact]
        public async Task Reset_ClearsPreviouslyRecordedSessionsAndReservations()
        {
            var store = new InMemoryRockPaperScissorsStore();
            var context = new InMemoryRockPaperScissorsContext(store);
            await context.TryReserveConnectionAsync("connection-1");
            await context.CreateAsync(RockPaperScissorsGameSessionRecord.Create(
                new Player("Alice", PlayerType.Human, MoveKind.Rock),
                new Player("Computer", PlayerType.Computer, MoveKind.Scissor)));

            store.Reset();

            Assert.Empty(await context.GetAllAsync<RockPaperScissorsGameSessionRecord>());
            Assert.True(await context.TryReserveConnectionAsync("connection-1"));
        }
    }
}
