using System.Security.Authentication;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.Channels.Chat.Models.Users
{
    internal
#if !DEBUG
        sealed
#endif
        class UserService(IChatContext chatContext)
    {
        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => chatContext.GetAsync<User, Guid>(userId, cancellationToken);

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
            => chatContext.GetAsync<User>(x => x.UserName == username, cancellationToken);

        public async Task<User> RegisterAsync(string username, string password, string name, CancellationToken cancellationToken = default)
        {
            var dbUser = await GetByUsernameAsync(username, cancellationToken);
            if (dbUser is not null)
                throw new InvalidOperationException("Username already exists");

            var user = User.Create(username, password, name);

            return await chatContext.CreateAsync(user, cancellationToken);
        }

        public async Task<User> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var user = await GetByUsernameAsync(username, cancellationToken);

            if (user is null || user.Password != password)
                throw new InvalidCredentialException();

            return user;
        }

        public async Task<IReadOnlyCollection<Group>> GetUserGroupsAsync(Guid id, CancellationToken cancellationToken = default)
            => await chatContext.GetAllAsync<Group>(x => x.GroupUsers.Any(y => y.UserId == id), cancellationToken);

        public async Task<IReadOnlyCollection<User>> GetUserContactsAsync(Guid id, CancellationToken cancellationToken = default)
            => (await chatContext.GetAllAsync<Message>(x => x.ReceiverId == id, cancellationToken))
                .Select(x => x.Sender)
                .Distinct()
                .ToList();

        public async Task<User> UpdateAsync(Guid userId, string bio, DateOnly? birthDate, CancellationToken cancellationToken = default)
        {
            var user = await chatContext.GetAsync<User, Guid>(userId, cancellationToken: cancellationToken) ?? throw new UserNotFoundException();

            user.SetBio(bio);

            user.SetBirthDate(birthDate);

            await chatContext.UpdateAsync(user, cancellationToken);

            return user;
        }

        public async Task SetNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
        {
            var user = await chatContext.GetAsync<User, Guid>(userId, cancellationToken: cancellationToken) ?? throw new UserNotFoundException();

            user.SetName(name);

            await chatContext.UpdateAsync(user, cancellationToken);
        }

        public async Task SetAvatarAsync(Guid userId, string avatar, CancellationToken cancellationToken = default)
        {
            var user = await chatContext.GetAsync<User, Guid>(userId, cancellationToken: cancellationToken) ?? throw new UserNotFoundException();

            user.SetAvatar(avatar);

            await chatContext.UpdateAsync(user, cancellationToken);
        }
    }
}