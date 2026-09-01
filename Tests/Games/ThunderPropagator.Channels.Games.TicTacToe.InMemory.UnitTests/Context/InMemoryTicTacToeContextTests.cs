using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;
using ThunderPropagator.Channels.Games.TicTacToe.InMemory.Context;
using ThunderPropagator.Channels.Games.TicTacToe.Models;

namespace ThunderPropagator.Channels.Games.TicTacToe.InMemory.UnitTests.Context
{
    /// <summary>
    /// Issue: contract coverage for <see cref="InMemoryTicTacToeContext"/>/<see cref="InMemoryTicTacToeStore"/>
    /// — mirrors ThunderPropagator.Channels.Games.RockPaperScissors.InMemory's own
    /// InMemoryRockPaperScissorsContextTests, extended with UpdateAsync coverage since
    /// TicTacToeGameRecord (unlike RockPaperScissors' own records) is mutated in place on every move.
    /// </summary>
    public sealed class InMemoryTicTacToeContextTests
    {
        private static InMemoryTicTacToeContext CreateContext() => new(new InMemoryTicTacToeStore());

        private static TicTacToeGameRecord CreateWaitingRecord(string sessionId = "session-1")
            => TicTacToeGameRecord.CreateWaitingForOpponent(sessionId, "---------", "Alice", PlayerSign.X, "connection-1");

        [Fact]
        public async Task CreateAsync_ThenGetAsync_ReturnsTheCreatedGame()
        {
            var context = CreateContext();
            var record = CreateWaitingRecord();

            await context.CreateAsync(record);

            var fetched = await context.GetAsync<TicTacToeGameRecord, string>("session-1");
            Assert.NotNull(fetched);
            Assert.Equal("Alice", fetched.Player1Name);
        }

        [Fact]
        public async Task GetAsync_ForAnUnknownId_ReturnsNull()
        {
            var context = CreateContext();

            Assert.Null(await context.GetAsync<TicTacToeGameRecord, string>("unknown-session"));
        }

        [Fact]
        public async Task UpdateAsync_PersistsTheMutatedRecord()
        {
            var context = CreateContext();
            var record = CreateWaitingRecord();
            await context.CreateAsync(record);

            record.Start("X--------", "Bob", PlayerKind.Human, "connection-2", null, PlayerSign.X);
            await context.UpdateAsync(record);

            var fetched = await context.GetAsync<TicTacToeGameRecord, string>("session-1");
            Assert.Equal("X--------", fetched!.Board);
            Assert.Equal("Bob", fetched.Player2Name);
        }

        [Fact]
        public async Task DeleteAsync_RemovesTheGame_AndReturnsTrueOnlyTheFirstTime()
        {
            var context = CreateContext();
            await context.CreateAsync(CreateWaitingRecord());

            Assert.True(await context.DeleteAsync<TicTacToeGameRecord, string>("session-1"));
            Assert.False(await context.DeleteAsync<TicTacToeGameRecord, string>("session-1"));
            Assert.Null(await context.GetAsync<TicTacToeGameRecord, string>("session-1"));
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEveryCreatedGame()
        {
            var context = CreateContext();
            await context.CreateAsync(CreateWaitingRecord("session-1"));
            await context.CreateAsync(CreateWaitingRecord("session-2"));

            var games = await context.GetAllAsync<TicTacToeGameRecord>();

            Assert.Equal(2, games.Count);
        }

        [Fact]
        public async Task Reset_ClearsPreviouslyCreatedGames()
        {
            var store = new InMemoryTicTacToeStore();
            var context = new InMemoryTicTacToeContext(store);
            await context.CreateAsync(CreateWaitingRecord());

            store.Reset();

            Assert.Empty(await context.GetAllAsync<TicTacToeGameRecord>());
        }
    }
}
