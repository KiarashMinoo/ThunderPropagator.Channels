using ThunderPropagator.Channels.Games.RockPaperScissors;
using ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.Context;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.UnitTests.Games.RockPaperScissors.EntityFrameworkCore.Context
{
    /// <summary>
    /// Issue #288: contract coverage for <see cref="EntityFrameworkCoreRockPaperScissorsContext"/> —
    /// mirrors ThunderPropagator.Channels.Chat.EntityFrameworkCore.UnitTests' own integration tests.
    /// Each test creates its own <see cref="ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.Context.RockPaperScissorsDbContext"/>
    /// against the fixture's already-migrated shared connection.
    /// </summary>
    public sealed class EntityFrameworkCoreRockPaperScissorsContextTests(RockPaperScissorsDatabaseFixture fixture) : IClassFixture<RockPaperScissorsDatabaseFixture>
    {
        [Fact]
        public async Task TryReserveConnectionAsync_FirstCall_ReturnsTrue()
        {
            var context = new EntityFrameworkCoreRockPaperScissorsContext(fixture.CreateDbContext());

            Assert.True(await context.TryReserveConnectionAsync($"connection-{Guid.NewGuid():N}"));
        }

        [Fact]
        public async Task TryReserveConnectionAsync_CalledTwiceForTheSameConnection_TheSecondCallReturnsFalse()
        {
            var connectionId = $"connection-{Guid.NewGuid():N}";

            Assert.True(await new EntityFrameworkCoreRockPaperScissorsContext(fixture.CreateDbContext()).TryReserveConnectionAsync(connectionId));
            Assert.False(await new EntityFrameworkCoreRockPaperScissorsContext(fixture.CreateDbContext()).TryReserveConnectionAsync(connectionId));
        }

        [Fact]
        public async Task CreateAsync_ThenGetAllAsync_ReturnsTheCreatedSession()
        {
            var context = new EntityFrameworkCoreRockPaperScissorsContext(fixture.CreateDbContext());
            var session = RockPaperScissorsGameSessionRecord.Create(
                new Player("Alice", PlayerType.Human, MoveKind.Rock),
                new Player("Computer", PlayerType.Computer, MoveKind.Scissor));

            await context.CreateAsync(session);

            var sessions = await new EntityFrameworkCoreRockPaperScissorsContext(fixture.CreateDbContext()).GetAllAsync<RockPaperScissorsGameSessionRecord>();
            Assert.Contains(sessions, s => s.SessionId == session.SessionId);
        }

        [Fact]
        public async Task GetAsync_ForAnUnknownId_ReturnsNull()
        {
            var context = new EntityFrameworkCoreRockPaperScissorsContext(fixture.CreateDbContext());

            Assert.Null(await context.GetAsync<RockPaperScissorsGameSessionRecord, string>($"unknown-{Guid.NewGuid():N}"));
        }
    }
}
