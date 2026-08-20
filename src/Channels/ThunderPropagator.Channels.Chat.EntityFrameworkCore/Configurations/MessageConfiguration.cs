using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.Channels.Chat.EntityFrameworkCore.Configurations
{
    internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages");

            builder.HasKey(message => message.Id);

            // See UserConfiguration's Id property for why this is necessary.
            builder.Property(message => message.Id)
                .ValueGeneratedNever();

            builder.Property(message => message.Body)
                .IsRequired();

            // Issue #117: stored as UTC ticks rather than the default formatted-text representation
            // so GetDirectMessageHistoryAsync/GetGroupMessageHistoryAsync can order by it server-side
            // on every relational provider — SQLite's EF Core provider outright refuses to translate
            // an ORDER BY over a DateTimeOffset column, a limitation SQL Server/PostgreSQL don't have,
            // but ticks sort correctly everywhere and this repo's own SQLite-backed integration tests
            // need it too. Lossless: every Message.Created is already DateTimeOffset.UtcNow (see
            // Message's constructor), so round-tripping through UTC ticks with a zero offset changes
            // nothing observable.
            builder.Property(message => message.Created)
                .HasConversion(created => created.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero))
                .IsRequired();

            // Issue #119: soft-delete state. DeletedAt uses the same UTC-ticks conversion as Created
            // for consistency, even though nothing currently orders by it.
            builder.Property(message => message.IsDeleted)
                .IsRequired();

            builder.Property(message => message.DeletedAt)
                .HasConversion(
                    deletedAt => deletedAt.HasValue ? deletedAt.Value.UtcTicks : (long?)null,
                    ticks => ticks.HasValue ? new DateTimeOffset(ticks.Value, TimeSpan.Zero) : null);

            // Issue #120: edit metadata, same shape as the soft-delete pair above.
            builder.Property(message => message.IsEdited)
                .IsRequired();

            builder.Property(message => message.EditedAt)
                .HasConversion(
                    editedAt => editedAt.HasValue ? editedAt.Value.UtcTicks : (long?)null,
                    ticks => ticks.HasValue ? new DateTimeOffset(ticks.Value, TimeSpan.Zero) : null);

            // Issue #125: read-receipt state, same shape as the soft-delete/edit pairs above.
            builder.Property(message => message.IsRead)
                .IsRequired();

            builder.Property(message => message.ReadAt)
                .HasConversion(
                    readAt => readAt.HasValue ? readAt.Value.UtcTicks : (long?)null,
                    ticks => ticks.HasValue ? new DateTimeOffset(ticks.Value, TimeSpan.Zero) : null);

            // Sender and Receiver both reference Users. SQL Server rejects Cascade on both (a
            // deleted user would reach the Messages table via two different paths), and there's
            // no existing delete-user operation that would need the cascade anyway, so both are
            // Restrict.
            builder.HasOne(message => message.Sender)
                .WithMany()
                .HasForeignKey(message => message.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(message => message.Receiver)
                .WithMany()
                .HasForeignKey(message => message.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(message => message.Group)
                .WithMany()
                .HasForeignKey(message => message.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // Message.Sender is public API — keep it populated for any consumer that reads a Message
            // directly and displays its sender. GetUserContactsAsync no longer depends on this: #115
            // moved it to a server-side SenderId/ReceiverId projection that never touches this
            // navigation.
            builder.Navigation(message => message.Sender)
                .AutoInclude();
        }
    }
}
