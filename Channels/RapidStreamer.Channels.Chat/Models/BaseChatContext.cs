using Microsoft.EntityFrameworkCore;
using RapidStreamer.Channels.Chat.Models.Groups;
using RapidStreamer.Channels.Chat.Models.Messages;
using RapidStreamer.Channels.Chat.Models.Users;

namespace RapidStreamer.Channels.Chat.Models
{
    internal interface IChatContext
    {
        DbSet<User> Users { get; set; }
        DbSet<Group> Groups { get; set; }
        DbSet<Message> Messages { get; set; }

        int SaveChanges();
        int SaveChanges(bool acceptAllChangesOnSuccess);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);
    }

    public abstract class BaseChatContext<TChatContext> : DbContext, IChatContext
        where TChatContext : BaseChatContext<TChatContext>
    {
        private static volatile bool IsInitialized;
        private static readonly object Mutex = new();

        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected BaseChatContext(DbContextOptions<TChatContext> options) : base(options)
        {
            if (IsInitialized)
                return;

            lock (Mutex)
            {
                if (IsInitialized)
                    return;

                Database.Migrate();
                Seed();
                IsInitialized = true;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("Chat");

            modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new GroupEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new MessageEntityTypeConfiguration());
        }

        protected abstract void Seed();
    }
}