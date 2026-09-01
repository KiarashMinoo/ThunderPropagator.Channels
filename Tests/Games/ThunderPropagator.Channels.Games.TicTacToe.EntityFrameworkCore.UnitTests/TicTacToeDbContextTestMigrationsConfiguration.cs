namespace ThunderPropagator.UnitTests.Games.TicTacToe.EntityFrameworkCore
{
    /// <summary>
    /// Single named place this test project's own SQLite migration setup is configured from — mirrors
    /// ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.UnitTests' own
    /// RockPaperScissorsDbContextTestMigrationsConfiguration.
    /// </summary>
    internal static class TicTacToeDbContextTestMigrationsConfiguration
    {
        public static readonly string MigrationsAssembly = typeof(TicTacToeDbContextTestMigrationsConfiguration).Assembly.GetName().Name!;

        public const string DesignTimeConnectionString = "Data Source=design-time.db";
    }
}
