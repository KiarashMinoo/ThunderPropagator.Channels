namespace ThunderPropagator.UnitTests.Games.RockPaperScissors.EntityFrameworkCore
{
    /// <summary>
    /// Single named place this test project's own SQLite migration setup is configured from — mirrors
    /// ThunderPropagator.Channels.Chat.EntityFrameworkCore.UnitTests' own ChatDbContextTestMigrationsConfiguration.
    /// </summary>
    internal static class RockPaperScissorsDbContextTestMigrationsConfiguration
    {
        public static readonly string MigrationsAssembly = typeof(RockPaperScissorsDbContextTestMigrationsConfiguration).Assembly.GetName().Name!;

        public const string DesignTimeConnectionString = "Data Source=design-time.db";
    }
}
