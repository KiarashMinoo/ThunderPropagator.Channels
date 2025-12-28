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
        public Group Group { get; private set; } = null!;
        public Guid UserId { get; }
        public User User { get; private set; } = null!;

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