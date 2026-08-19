using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.EntityFrameworkCore.Configurations
{
    internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(user => user.Id);

            // User.Id is set client-side (Guid.NewGuid() in the constructor), never by the
            // database. Without this, EF's default Guid convention (ValueGeneratedOnAdd) treats a
            // non-default key as "might already exist", so SaveChanges can issue an UPDATE for a
            // brand-new row instead of an INSERT and fail with a concurrency exception.
            builder.Property(user => user.Id)
                .ValueGeneratedNever();

            builder.Property(user => user.UserName)
                .IsRequired()
                .HasMaxLength(256);

            builder.HasIndex(user => user.UserName)
                .IsUnique();

            builder.Property(user => user.PasswordHash)
                .IsRequired();

            builder.Property(user => user.Name)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(user => user.Avatar);

            builder.Property(user => user.Bio);

            builder.Property(user => user.BirthDate);
        }
    }
}
