using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.Channels.Chat.EntityFrameworkCore.Configurations
{
    internal sealed class GroupUserConfiguration : IEntityTypeConfiguration<GroupUser>
    {
        public void Configure(EntityTypeBuilder<GroupUser> builder)
        {
            builder.ToTable("GroupUsers");

            builder.HasKey(groupUser => groupUser.Id);

            // See UserConfiguration's Id property for why this is necessary — it's what actually
            // makes Group.AddUser's newly-created GroupUser insert instead of trying (and failing)
            // to update a row that was never there.
            builder.Property(groupUser => groupUser.Id)
                .ValueGeneratedNever();

            // Group.AddUser has no in-memory duplicate check (GroupUser doesn't override
            // Equals/GetHashCode, so the backing HashSet dedupes by reference, not by value) —
            // this constraint is the actual guarantee that a user can't join the same group twice.
            builder.HasIndex(groupUser => new { groupUser.GroupId, groupUser.UserId })
                .IsUnique();

            // Issue #142: .IsRequired() makes explicit what UserId being a non-nullable Guid column
            // already implied by convention. Never AutoInclude'd — GroupUser.User is never populated
            // by any provider; see its own doc comment. A user's memberships are always reached
            // through Group.GroupUsers (which every provider does guarantee populated) or by
            // UserId/GroupId directly, not through this back-reference.
            builder.HasOne(groupUser => groupUser.User)
                .WithMany()
                .HasForeignKey(groupUser => groupUser.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
