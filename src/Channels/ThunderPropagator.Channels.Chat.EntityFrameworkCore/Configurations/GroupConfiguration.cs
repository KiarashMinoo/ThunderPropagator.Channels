using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.Channels.Chat.EntityFrameworkCore.Configurations
{
    internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.ToTable("Groups");

            builder.HasKey(group => group.Id);

            // See UserConfiguration's Id property for why this is necessary.
            builder.Property(group => group.Id)
                .ValueGeneratedNever();

            builder.Property(group => group.Name)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(group => group.GroupIcon);

            // Issue #124: CreatedByUserId is set once at creation (Group has no setter for it) —
            // this domain's only admin concept. IsDeleted/DeletedAt are the same soft-delete shape
            // as Message's (#119); DeletedAt uses the same UTC-ticks conversion as
            // Message.Created/DeletedAt/EditedAt for consistency, even though nothing currently
            // orders by it.
            builder.Property(group => group.CreatedByUserId)
                .IsRequired();

            builder.Property(group => group.IsDeleted)
                .IsRequired();

            builder.Property(group => group.DeletedAt)
                .HasConversion(
                    deletedAt => deletedAt.HasValue ? deletedAt.Value.UtcTicks : (long?)null,
                    ticks => ticks.HasValue ? new DateTimeOffset(ticks.Value, TimeSpan.Zero) : null);

            // Group.AddUser/RemoveUser mutate the private _groupUsers field directly, so the
            // navigation must be configured to read/write through that field rather than the
            // read-only GroupUsers property.
            builder.HasMany(group => group.GroupUsers)
                .WithOne(groupUser => groupUser.Group)
                .HasForeignKey(groupUser => groupUser.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(group => group.GroupUsers)
                .HasField("_groupUsers")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                // MessageService.SendMessageToGroupAsync enumerates group.GroupUsers in memory
                // after loading a Group by id, so it must always be populated.
                .AutoInclude();
        }
    }
}
