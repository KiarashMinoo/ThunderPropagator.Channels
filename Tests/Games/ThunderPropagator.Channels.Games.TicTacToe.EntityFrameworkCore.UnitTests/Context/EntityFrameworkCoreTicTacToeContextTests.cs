using ThunderPropagator.Channels.Games.TicTacToe.EntityFrameworkCore.Context;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;
using ThunderPropagator.Channels.Games.TicTacToe.Models;

namespace ThunderPropagator.UnitTests.Games.TicTacToe.EntityFrameworkCore.Context
{
    /// <summary>
    /// CRUD roundtrip coverage for <see cref="EntityFrameworkCoreTicTacToeContext"/> against a real
    /// SQLite-backed <see cref="TicTacToeDbContext"/> — mirrors
    /// ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.UnitTests' own
    /// EntityFrameworkCoreRockPaperScissorsContextTests.
    /// </summary>
    public sealed class EntityFrameworkCoreTicTacToeContextTests(TicTacToeDatabaseFixture fixture)
        : IClassFixture<TicTacToeDatabaseFixture>
    {
        private static TicTacToeGameRecord CreateWaitingRecord(string sessionId)
            => TicTacToeGameRecord.CreateWaitingForOpponent(sessionId, "---------", "Alice", PlayerSign.X, "connection-1");

        [Fact]
        public async Task CreateAsync_ThenGetAsync_ReturnsTheCreatedGame()
        {
            var sessionId = Guid.NewGuid().ToString();
            using var dbContext = fixture.CreateDbContext();
            var context = new EntityFrameworkCoreTicTacToeContext(dbContext);

            await context.CreateAsync(CreateWaitingRecord(sessionId));

            using var readDbContext = fixture.CreateDbContext();
            var readContext = new EntityFrameworkCoreTicTacToeContext(readDbContext);
            var fetched = await readContext.GetAsync<TicTacToeGameRecord, string>(sessionId);
            Assert.NotNull(fetched);
            Assert.Equal("Alice", fetched.Player1Name);
        }

        [Fact]
        public async Task GetAsync_ForAnUnknownId_ReturnsNull()
        {
            using var dbContext = fixture.CreateDbContext();
            var context = new EntityFrameworkCoreTicTacToeContext(dbContext);

            Assert.Null(await context.GetAsync<TicTacToeGameRecord, string>(Guid.NewGuid().ToString()));
        }

        [Fact]
        public async Task UpdateAsync_PersistsTheMutatedRecord()
        {
            var sessionId = Guid.NewGuid().ToString();
            using (var dbContext = fixture.CreateDbContext())
            {
                await new EntityFrameworkCoreTicTacToeContext(dbContext).CreateAsync(CreateWaitingRecord(sessionId));
            }

            using (var dbContext = fixture.CreateDbContext())
            {
                var context = new EntityFrameworkCoreTicTacToeContext(dbContext);
                var record = await context.GetAsync<TicTacToeGameRecord, string>(sessionId);
                record!.Start("X--------", "Bob", PlayerKind.Human, "connection-2", null, PlayerSign.X);
                await context.UpdateAsync(record);
            }

            using (var dbContext = fixture.CreateDbContext())
            {
                var fetched = await new EntityFrameworkCoreTicTacToeContext(dbContext)
                    .GetAsync<TicTacToeGameRecord, string>(sessionId);
                Assert.Equal("X--------", fetched!.Board);
                Assert.Equal("Bob", fetched.Player2Name);
            }
        }

        [Fact]
        public async Task DeleteAsync_RemovesTheGame_AndReturnsTrueOnlyTheFirstTime()
        {
            var sessionId = Guid.NewGuid().ToString();
            using var dbContext = fixture.CreateDbContext();
            var context = new EntityFrameworkCoreTicTacToeContext(dbContext);
            await context.CreateAsync(CreateWaitingRecord(sessionId));

            Assert.True(await context.DeleteAsync<TicTacToeGameRecord, string>(sessionId));
            Assert.False(await context.DeleteAsync<TicTacToeGameRecord, string>(sessionId));
            Assert.Null(await context.GetAsync<TicTacToeGameRecord, string>(sessionId));
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEveryCreatedGame()
        {
            var sessionId1 = Guid.NewGuid().ToString();
            var sessionId2 = Guid.NewGuid().ToString();
            using var dbContext = fixture.CreateDbContext();
            var context = new EntityFrameworkCoreTicTacToeContext(dbContext);
            await context.CreateAsync(CreateWaitingRecord(sessionId1));
            await context.CreateAsync(CreateWaitingRecord(sessionId2));

            var games = await context.GetAllAsync<TicTacToeGameRecord>();

            Assert.Contains(games, g => g.SessionId == sessionId1);
            Assert.Contains(games, g => g.SessionId == sessionId2);
        }
    }
}
