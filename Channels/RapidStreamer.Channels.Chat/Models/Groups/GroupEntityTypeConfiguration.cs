using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RapidStreamer.Channels.Chat.Models.Groups
{
    internal sealed class GroupEntityTypeConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.ToTable("Groups", "Chat");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired();
            builder.HasIndex(x => x.Name).IsUnique();

            builder
                .OwnsMany(x => x.GroupUsers, groupUser =>
                {
                    groupUser.ToTable("GroupUsers", "Chat");

                    groupUser.HasKey(x => x.Id);

                    groupUser.Property(x => x.GroupId).IsRequired();
                    groupUser.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId);

                    groupUser.Property(x => x.UserId).IsRequired();
                    groupUser.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);

                    groupUser
                        .HasIndex(x => new
                        {
                            x.GroupId,
                            x.UserId
                        }).IsUnique();
                })
                .Navigation(x => x.GroupUsers)
                .HasField("_groupUsers");
        }
    }
}