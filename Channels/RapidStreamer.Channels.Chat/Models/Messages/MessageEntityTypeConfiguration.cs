using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RapidStreamer.Channels.Chat.Models.Messages
{
    internal sealed class MessageEntityTypeConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages", "Chat");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.SenderId).IsRequired();
            builder.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId);

            builder.Property(x => x.ReceiverId).IsRequired();
            builder.HasOne(x => x.Receiver).WithMany().HasForeignKey(x => x.ReceiverId);

            builder.Property(x => x.GroupId);
            builder.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId);

            builder.Property(x => x.Body).IsRequired();
        }
    }
}