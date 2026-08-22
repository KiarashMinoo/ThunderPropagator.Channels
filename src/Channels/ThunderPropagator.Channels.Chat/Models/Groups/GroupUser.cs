using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.Models.Groups
{
    public
#if !DEBUG
        sealed
#endif
        class GroupUser
    {
        public Guid Id { get; }
        public Guid GroupId { get; }

        // Issue #142: not part of any provider's loading contract — no provider intentionally
        // populates it (a GroupUser is only ever reached through Group.GroupUsers, which every
        // provider does guarantee populated, see Group.GroupUsers' own doc comment, or by
        // GroupId/UserId directly), so InMemory and MongoDB always leave it null. EntityFrameworkCore
        // is the one exception: its change-tracking incidentally fixes this reference up whenever a
        // Group and its GroupUsers are loaded together in the same tracked DbContext, purely as a
        // side effect of that provider's tracking mechanics, not a deliberate populate step. Never
        // rely on this being either null or populated — resolve the owning Group via GroupId instead.
        public Group? Group { get; private set; }

        public Guid UserId { get; }

        // Issue #142: also never populated by any provider — resolve via UserId
        // (e.g. IChatContext.GetAsync<User, Guid>) if actually needed.
        public User? User { get; private set; }

        private GroupUser()
        {
            Id = Guid.NewGuid();
        }

        private GroupUser(Guid groupId, Guid userId) : this()
        {
            GroupId = groupId;
            UserId = userId;
        }

        internal static GroupUser Create(Guid groupId, Guid userId) => new(groupId, userId);
    }
}