namespace ThunderPropagator.UnitTests.Channels.Chat.EntityFrameworkCore
{
    /// <summary>
    /// Single named place this test project's own SQLite migration setup is configured from —
    /// both ChatDbContextDesignTimeFactory (design-time, used by `dotnet ef migrations add`) and
    /// ChatDatabaseFixture (runtime, used by the integration tests) read the migrations assembly and
    /// design-time connection string from here rather than each hardcoding its own copy, so there is
    /// exactly one place to look — or change — if this test project's migration setup ever needs to
    /// move.
    /// </summary>
    internal static class ChatDbContextTestMigrationsConfiguration
    {
        /// <summary>
        /// Keeps the scaffolded migrations in this test project's own assembly instead of the shipped
        /// ChatDbContext's assembly (EF's default) — see ChatDbContext's own doc comment for why no
        /// migrations ship with that package at all.
        /// </summary>
        public static readonly string MigrationsAssembly = typeof(ChatDbContextTestMigrationsConfiguration).Assembly.GetName().Name!;

        /// <summary>
        /// Only ever opened by `dotnet ef migrations add`/`dotnet ef database update` at design time
        /// to compute the scaffolded migration's Up/Down — never a database the test suite itself
        /// reads from (ChatDatabaseFixture uses its own ":memory:" connection instead).
        /// </summary>
        public const string DesignTimeConnectionString = "Data Source=design-time.db";
    }
}
