using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using ThunderPropagator.Channels.Chat;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Configuration;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models
{
    /// <summary>
    /// Issue #38: RegisterAsync/SetNameAsync/SetAvatarAsync/UpdateAsync used to accept a
    /// username/password/display name/avatar/bio of any length — the same unbounded-input gap
    /// MaxMessageLength (#141) already closed for message Body. There's no concrete IChatContext
    /// provider in this repo yet, so this uses a minimal in-memory fake, mirroring
    /// UserServiceAuthenticationTests' own FakeChatContext.
    /// </summary>
    public sealed class UserServiceValidationTests
    {
        private sealed class FakeChatContext : IChatContext
        {
            private readonly List<User> _users = [];

            public void Seed(User user) => _users.Add(user);

            public Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => Task.FromResult(_users.OfType<TEntity>().AsQueryable().FirstOrDefault(expression));

            public Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => Task.FromResult((TEntity?)(object?)_users.SingleOrDefault(user => user.Id.Equals(id)));

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
            {
                _users.Add((User)(object)entity!);
                return Task.FromResult(entity);
            }

            public Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
                => Task.FromResult(entity);

            public Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<IReadOnlyCollection<User>> GetContactsAsync(Guid userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<MessageHistoryPage> GetDirectMessageHistoryAsync(Guid userId, Guid otherUserId, int page, int pageSize, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<MessageHistoryPage> GetGroupMessageHistoryAsync(Guid groupId, int page, int pageSize, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<UserSearchPage> SearchUsersAsync(string normalizedTerm, int page, int pageSize, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        private static (UserService Service, FakeChatContext Context, ChatChannelConfiguration Configuration) CreateService()
        {
            var context = new FakeChatContext();
            var configuration = new ChatChannelConfiguration();
            var service = new UserService(context, new PasswordHasher<User>(), configuration);
            return (service, context, configuration);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RegisterAsync_WithAnEmptyUsername_ThrowsInvalidUserRegistrationRequestException(string username)
        {
            var (service, _, _) = CreateService();

            await Assert.ThrowsAsync<InvalidUserRegistrationRequestException>(
                () => service.RegisterAsync(username, "correct horse battery staple", "Alice"));
        }

        [Fact]
        public async Task RegisterAsync_WithAUsernameOverMaxUserNameLength_ThrowsInvalidUserRegistrationRequestException()
        {
            var (service, _, configuration) = CreateService();
            configuration.MaxUserNameLength = 10;

            await Assert.ThrowsAsync<InvalidUserRegistrationRequestException>(
                () => service.RegisterAsync(new string('a', 11), "correct horse battery staple", "Alice"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RegisterAsync_WithAnEmptyPassword_ThrowsInvalidUserRegistrationRequestException(string password)
        {
            var (service, _, _) = CreateService();

            await Assert.ThrowsAsync<InvalidUserRegistrationRequestException>(
                () => service.RegisterAsync("alice", password, "Alice"));
        }

        [Fact]
        public async Task RegisterAsync_WithAPasswordOverMaxPasswordLength_ThrowsInvalidUserRegistrationRequestException()
        {
            var (service, _, configuration) = CreateService();
            configuration.MaxPasswordLength = 10;

            await Assert.ThrowsAsync<InvalidUserRegistrationRequestException>(
                () => service.RegisterAsync("alice", new string('a', 11), "Alice"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RegisterAsync_WithAnEmptyName_ThrowsInvalidUserRegistrationRequestException(string name)
        {
            var (service, _, _) = CreateService();

            await Assert.ThrowsAsync<InvalidUserRegistrationRequestException>(
                () => service.RegisterAsync("alice", "correct horse battery staple", name));
        }

        [Fact]
        public async Task RegisterAsync_WithANameOverMaxDisplayNameLength_ThrowsInvalidUserRegistrationRequestException()
        {
            var (service, _, configuration) = CreateService();
            configuration.MaxDisplayNameLength = 10;

            await Assert.ThrowsAsync<InvalidUserRegistrationRequestException>(
                () => service.RegisterAsync("alice", "correct horse battery staple", new string('a', 11)));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SetNameAsync_WithAnEmptyName_ThrowsInvalidUserProfileRequestException(string name)
        {
            var (service, context, _) = CreateService();
            var user = User.Create("alice", "Alice");
            context.Seed(user);

            await Assert.ThrowsAsync<InvalidUserProfileRequestException>(
                () => service.SetNameAsync(user.Id, name));
        }

        [Fact]
        public async Task SetNameAsync_WithANameOverMaxDisplayNameLength_ThrowsInvalidUserProfileRequestException()
        {
            var (service, context, configuration) = CreateService();
            configuration.MaxDisplayNameLength = 10;
            var user = User.Create("alice", "Alice");
            context.Seed(user);

            await Assert.ThrowsAsync<InvalidUserProfileRequestException>(
                () => service.SetNameAsync(user.Id, new string('a', 11)));
        }

        [Fact]
        public async Task SetAvatarAsync_WithAnAvatarOverMaxAvatarLength_ThrowsInvalidUserProfileRequestException()
        {
            var (service, context, configuration) = CreateService();
            configuration.MaxAvatarLength = 10;
            var user = User.Create("alice", "Alice");
            context.Seed(user);

            await Assert.ThrowsAsync<InvalidUserProfileRequestException>(
                () => service.SetAvatarAsync(user.Id, new string('a', 11)));
        }

        [Fact]
        public async Task SetAvatarAsync_WithAnEmptyAvatar_Succeeds()
        {
            var (service, context, _) = CreateService();
            var user = User.Create("alice", "Alice");
            context.Seed(user);

            var exception = await Record.ExceptionAsync(() => service.SetAvatarAsync(user.Id, string.Empty));

            Assert.Null(exception);
        }

        [Fact]
        public async Task UpdateAsync_WithABioOverMaxBioLength_ThrowsInvalidUserProfileRequestException()
        {
            var (service, context, configuration) = CreateService();
            configuration.MaxBioLength = 10;
            var user = User.Create("alice", "Alice");
            context.Seed(user);

            await Assert.ThrowsAsync<InvalidUserProfileRequestException>(
                () => service.UpdateAsync(user.Id, new string('a', 11), null));
        }

        [Fact]
        public async Task UpdateAsync_WithAnEmptyBio_Succeeds()
        {
            var (service, context, _) = CreateService();
            var user = User.Create("alice", "Alice");
            context.Seed(user);

            var exception = await Record.ExceptionAsync(() => service.UpdateAsync(user.Id, string.Empty, null));

            Assert.Null(exception);
        }
    }
}
