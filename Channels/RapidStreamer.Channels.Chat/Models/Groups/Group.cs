namespace RapidStreamer.Channels.Chat.Models.Groups
{
    public
#if !DEBUG
        sealed
#endif
        class Group
    {
        public Guid Id { get; }
        public string Name { get; } = null!;

        private readonly HashSet<GroupUser> _groupUsers = [];
        public IReadOnlyCollection<GroupUser> GroupUsers => _groupUsers;

        private Group()
        {
            Id = Guid.NewGuid();
        }

        private Group(string name) : this()
        {
            Name = name;
        }

        internal Group AddUser(Guid userId)
        {
            _groupUsers.Add(GroupUser.Create(Id, userId));
            return this;
        }

        internal Group RemoveUser(Guid userId)
        {
            var groupUser = _groupUsers.FirstOrDefault(gu => gu.UserId == userId);
            if (groupUser is not null)
                _groupUsers.Remove(groupUser);

            return this;
        }

        internal static Group Create(string name) => new(name);
    }
}