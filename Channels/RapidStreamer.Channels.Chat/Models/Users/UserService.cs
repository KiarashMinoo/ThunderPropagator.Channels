using System.Security.Authentication;
using Microsoft.EntityFrameworkCore;
using RapidStreamer.Channels.Chat.Models.Groups;

namespace RapidStreamer.Channels.Chat.Models.Users
{
    internal
#if !DEBUG
        sealed
#endif
        class UserService(IChatContext chatContext)
    {
        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => chatContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
            => chatContext.Users.SingleOrDefaultAsync(x => x.UserName == username, cancellationToken);

        public async Task<User> RegisterAsync(string username, string password, string name, CancellationToken cancellationToken = default)
        {
            var dbUser = await GetByUsernameAsync(username, cancellationToken);
            if (dbUser is not null)
                throw new InvalidOperationException("Username already exists");

            var user = User.Create(username, password, name);

            var entry = await chatContext.Users.AddAsync(user, cancellationToken);

            await chatContext.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }

        public async Task<User> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var user = await GetByUsernameAsync(username, cancellationToken);

            if (user is null)
                throw new InvalidCredentialException();

            if (user.Password != password)
                throw new InvalidCredentialException();

            return user;
        }

        public async Task<IReadOnlyCollection<Group>> GetUserGroupsAsync(Guid id, CancellationToken cancellationToken = default)
            => await chatContext.Groups.Where(x => x.GroupUsers.Any(y => y.UserId == id)).ToListAsync(cancellationToken);

        public async Task<IReadOnlyCollection<User>> GetUserContactsAsync(Guid id, CancellationToken cancellationToken = default)
            => await chatContext.Messages.Where(x => x.ReceiverId == id).Select(x => x.Sender).Distinct().ToListAsync(cancellationToken);
    }
}