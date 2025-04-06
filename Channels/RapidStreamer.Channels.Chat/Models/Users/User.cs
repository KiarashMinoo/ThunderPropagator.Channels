namespace RapidStreamer.Channels.Chat.Models.Users
{
    public
#if !DEBUG
        sealed
#endif
        class User
    {
        public Guid Id { get; }
        public string UserName { get; } = null!;
        public string Password { get; } = null!;
        public string Name { get; } = null!;
        public string? Avatar { get; private set; }
        public string? Bio { get; private set; }
        public DateTime? BirthDate { get; private set; }

        private User()
        {
            Id = Guid.NewGuid();
        }

        private User(string userName, string password, string name) : this()
        {
            UserName = userName;
            Password = password;
            Name = name;
        }

        internal User SetAvatar(string avatar)
        {
            Avatar = avatar;
            return this;
        }

        internal User SetBio(string bio)
        {
            Bio = bio;
            return this;
        }

        internal User SetBirthDate(DateTime birthDate)
        {
            BirthDate = birthDate;
            return this;
        }

        internal static User Create(string username, string password, string name) => new(username, password, name);
    }
}