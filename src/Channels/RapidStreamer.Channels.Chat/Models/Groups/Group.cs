namespace RapidStreamer.Channels.Chat.Models.Groups
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

        private readonly HashSet<GroupUser> _groupUsers = [];
        public IReadOnlyCollection<GroupUser> GroupUsers => _groupUsers;

        private Group()
        {
            Id = Guid.NewGuid();
        }

        private Group(string name) : this()
        {
            SetName(name);
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

        internal static Group Create(string name) => new(name);
    }
}