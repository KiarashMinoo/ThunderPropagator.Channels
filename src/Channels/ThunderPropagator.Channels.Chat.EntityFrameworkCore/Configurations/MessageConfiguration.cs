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

            builder.Property(message => message.Created)
                .IsRequired();

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
