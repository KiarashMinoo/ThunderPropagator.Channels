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

            // UserService.GetUserContactsAsync projects Message.Sender in memory after loading
            // messages by ReceiverId, so it must always be populated.
            builder.Navigation(message => message.Sender)
                .AutoInclude();
        }
    }
}
