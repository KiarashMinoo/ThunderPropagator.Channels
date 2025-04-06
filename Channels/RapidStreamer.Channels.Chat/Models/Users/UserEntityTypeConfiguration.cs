using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RapidStreamer.Channels.Chat.Models.Users
{
    internal sealed class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users", "Chat");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserName).IsRequired();
            builder.HasIndex(x => x.UserName).IsUnique();

            builder.Property(x => x.Password).IsRequired();
            builder.Property(x => x.Name).IsRequired();
            builder.Property(x => x.Avatar);
            builder.Property(x => x.Bio);
            builder.Property(x => x.BirthDate);
        }
    }
}