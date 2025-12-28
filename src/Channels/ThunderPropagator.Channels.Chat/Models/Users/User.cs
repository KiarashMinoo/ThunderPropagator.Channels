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
        public string Password { get; } = null!;

        public string Name { get; private set; } = null!;
        public string? Avatar { get; private set; }
        public string? Bio { get; private set; }
        public DateOnly? BirthDate { get; private set; }

        private User()
        {
            Id = Guid.NewGuid();
        }

        private User(string userName, string password, string name) : this()
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userName));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(password));

            UserName = userName;
            Password = password;
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

        internal static User Create(string username, string password, string name) => new(username, password, name);
    }
}