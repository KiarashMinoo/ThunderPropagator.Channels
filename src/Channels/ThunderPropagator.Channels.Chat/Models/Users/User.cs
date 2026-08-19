using System.Text.Json.Serialization;

namespace ThunderPropagator.Channels.Chat.Models.Users
{
    public
#if !DEBUG
        sealed
#endif
        class User
    {
        public Guid Id { get; }
        public string UserName { get; } = null!;

        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string PasswordHash { get; private set; } = null!;

        public string Name { get; private set; } = null!;
        public string? Avatar { get; private set; }
        public string? Bio { get; private set; }
        public DateOnly? BirthDate { get; private set; }

        private User()
        {
            Id = Guid.NewGuid();
        }

        private User(string userName, string name) : this()
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userName));

            UserName = userName;
            SetName(name);
        }

        internal User SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));

            Name = name;
            return this;
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

        internal User SetBirthDate(DateOnly? birthDate)
        {
            BirthDate = birthDate;
            return this;
        }

        internal User SetPasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(passwordHash));

            PasswordHash = passwordHash;
            return this;
        }

        internal static User Create(string username, string name) => new(username, name);
    }
}