using Microsoft.EntityFrameworkCore;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.EntityFrameworkCore
{
    /// <summary>
    /// The real EF Core <see cref="DbContext"/> for the Chat domain. This type is intentionally
    /// provider-agnostic: it configures relationships, keys, constraints, and indexes through
    /// <see cref="IEntityTypeConfiguration{TEntity}"/> classes in the Configurations folder, and
    /// relies on the caller (see <see cref="ChatEntityFrameworkCoreExtensions"/>) to select and
    /// configure a specific relational provider (SQL Server, PostgreSQL, MySQL, SQLite, ...) via
    /// <see cref="DbContextOptionsBuilder"/>. No migrations ship with this package for the same
    /// reason — a migration's generated SQL is tied to whichever provider was active when it was
    /// scaffolded, so baking one in here would silently break every other provider. Consumers scaffold
    /// their own migrations against their chosen provider once they've referenced this package.
    /// </summary>
    public sealed class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<GroupUser> GroupUsers => Set<GroupUser>();
        public DbSet<Message> Messages => Set<Message>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);
        }
    }
}
