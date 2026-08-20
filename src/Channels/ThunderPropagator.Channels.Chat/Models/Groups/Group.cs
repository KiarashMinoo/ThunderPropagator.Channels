namespace ThunderPropagator.Channels.Chat.Models.Groups
{
    public
#if !DEBUG
        sealed
#endif
        class Group
    {
        public Guid Id { get; }
        public string Name { get; private set; } = null!;
        public string? GroupIcon { get; private set; }

        // Issue #124: the only admin concept this domain has — whoever created the group. There is
        // no promote/demote mechanism (a broader admin-role system is a separate, future ticket), so
        // this is also, for now, the complete set of users authorized to delete the group or moderate
        // its messages (see MessageService.DeleteMessageAsync).
        public Guid CreatedByUserId { get; }

        // Issue #124: soft-delete state, same shape as Message's (#119) and for the same reason —
        // GroupUsers is cleared immediately on deletion (nobody is "a member" of a deleted group),
        // but the row itself is kept so Message.GroupId's foreign key never breaks: existing group
        // messages are retained exactly as written, not cascade-deleted or orphaned.
        public bool IsDeleted { get; private set; }
        public DateTimeOffset? DeletedAt { get; private set; }

        private readonly HashSet<GroupUser> _groupUsers = [];
        public IReadOnlyCollection<GroupUser> GroupUsers => _groupUsers;

        private Group()
        {
            Id = Guid.NewGuid();
        }

        private Group(string name, Guid createdByUserId) : this()
        {
            SetName(name);
            CreatedByUserId = createdByUserId;
        }

        internal Group AddUser(Guid userId)
        {
            _groupUsers.Add(GroupUser.Create(Id, userId));
            return this;
        }

        internal Group RemoveUser(Guid userId)
        {
            var groupUser = _groupUsers.SingleOrDefault(gu => gu.UserId == userId);
            if (groupUser is not null)
                _groupUsers.Remove(groupUser);

            return this;
        }

        internal Group SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));

            Name = name;
            return this;
        }

        internal Group SetGroupIcon(string icon)
        {
            GroupIcon = icon;
            return this;
        }

        // Idempotent by design, mirroring Message.MarkDeleted (#119): a second call is a no-op
        // rather than overwriting DeletedAt or re-clearing an already-empty GroupUsers.
        internal Group MarkDeleted()
        {
            if (IsDeleted)
                return this;

            IsDeleted = true;
            DeletedAt = DateTimeOffset.UtcNow;
            _groupUsers.Clear();
            return this;
        }

        internal static Group Create(string name, Guid createdByUserId) => new(name, createdByUserId);
    }
}
