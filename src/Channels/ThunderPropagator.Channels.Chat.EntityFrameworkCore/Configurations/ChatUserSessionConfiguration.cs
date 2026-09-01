using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThunderPropagator.Channels.Chat.Models.Sessions;

namespace ThunderPropagator.Channels.Chat.EntityFrameworkCore.Configurations
{
    internal sealed class ChatUserSessionConfiguration : IEntityTypeConfiguration<ChatUserSession>
    {
        public void Configure(EntityTypeBuilder<ChatUserSession> builder)
        {
            builder.ToTable("ChatUserSessions");

            builder.HasKey(session => session.Id);

            // See UserConfiguration's Id property for why this is necessary.
            builder.Property(session => session.Id)
                .ValueGeneratedNever();

            builder.Property(session => session.ConnectionId)
                .IsRequired()
                .HasMaxLength(256);

            // A connection is logged in as at most one user at a time — ChatUserSessionService
            // enforces the replace-not-reject behavior in application code (see its own comment), but
            // the index still guards against two rows for the same connectionId ever coexisting.
            builder.HasIndex(session => session.ConnectionId)
                .IsUnique();

            builder.Property(session => session.UserId)
                .IsRequired();

            builder.HasIndex(session => session.UserId);
        }
    }
}
