using System.Linq.Expressions;
using System.Security.Authentication;
using Microsoft.AspNetCore.Identity;
using ThunderPropagator.Channels.Chat;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Configuration;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models
{
    /// <summary>
    /// Issue #108: UserService used to store and compare passwords in plaintext (User.Password,
    /// LoginAsync's user.Password != password check). RegisterAsync now hashes through
    /// IPasswordHasher&lt;User&gt; before persisting, and LoginAsync verifies through the same hasher,
    /// treating a malformed stored hash the same as a wrong password so neither leaks which failure
    /// mode occurred. There's no concrete IChatContext provider in this repo yet (BaseChatContext has
    /// no subclass — EF Core/MongoDB/in-memory providers are separate tickets), so these tests use a
    /// minimal in-memory fake instead of a real database.
    /// </summary>
    public sealed class UserServiceAuthenticationTests
    {
        private sealed class FakeChatContext : IChatContext
        {
            private readonly List<User> _users = [];

            public void Seed(User user) => _users.Add(user);

            public Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => Task.FromResult(_users.OfType<TEntity>().AsQueryable().FirstOrDefault(expression));

            public Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

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

        private static (UserService Service, FakeChatContext Context) CreateService(bool allowGuestRegister = true)
        {
            var context = new FakeChatContext();
            var configuration = new ChatChannelConfiguration { AllowGuestRegister = allowGuestRegister };
            var service = new UserService(context, new PasswordHasher<User>(), configuration);
            return (service, context);
        }

        // Issue #141: a host that provisions users through its own admin/SSO flow can close off
        // self-service registration via ChatChannelConfiguration.AllowGuestRegister — checked before
        // the username-uniqueness lookup (UserService.RegisterAsync's own comment explains why).
        [Fact]
        public async Task RegisterAsync_WhenGuestRegistrationIsDisabled_ThrowsGuestRegistrationDisabledException()
        {
            var (service, _) = CreateService(allowGuestRegister: false);

            await Assert.ThrowsAsync<GuestRegistrationDisabledException>(
                () => service.RegisterAsync("alice", "correct horse battery staple", "Alice"));
        }

        [Fact]
        public async Task RegisterAsync_WhenGuestRegistrationIsEnabled_Succeeds()
        {
            var (service, _) = CreateService(allowGuestRegister: true);

            var user = await service.RegisterAsync("alice", "correct horse battery staple", "Alice");

            Assert.Equal("alice", user.UserName);
        }

        [Fact]
        public async Task RegisterAsync_StoresAHashThatDiffersFromThePlaintextPassword()
        {
            var (service, _) = CreateService();

            var user = await service.RegisterAsync("alice", "correct horse battery staple", "Alice");

            Assert.NotEqual("correct horse battery staple", user.PasswordHash);
            Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
        }

        [Fact]
        public async Task LoginAsync_WithTheCorrectPassword_ReturnsTheUser()
        {
            var (service, _) = CreateService();
            await service.RegisterAsync("alice", "correct horse battery staple", "Alice");

            var user = await service.LoginAsync("alice", "correct horse battery staple");

            Assert.Equal("alice", user.UserName);
        }

        [Fact]
        public async Task LoginAsync_WithTheWrongPassword_ThrowsInvalidCredentialException()
        {
            var (service, _) = CreateService();
            await service.RegisterAsync("alice", "correct horse battery staple", "Alice");

            await Assert.ThrowsAsync<InvalidCredentialException>(() => service.LoginAsync("alice", "wrong password"));
        }

        [Fact]
        public async Task LoginAsync_ForAnUnknownUsername_ThrowsInvalidCredentialException()
        {
            var (service, _) = CreateService();

            await Assert.ThrowsAsync<InvalidCredentialException>(() => service.LoginAsync("nobody", "anything"));
        }

        [Fact]
        public async Task LoginAsync_WithAMalformedStoredHash_ThrowsInvalidCredentialException_NotAFormatException()
        {
            var (service, context) = CreateService();
            var user = User.Create("alice", "Alice").SetPasswordHash("not-a-real-hash");
            context.Seed(user);

            await Assert.ThrowsAsync<InvalidCredentialException>(() => service.LoginAsync("alice", "anything"));
        }

        [Fact]
        public async Task LoginAsync_WhenTheHasherRequestsARehash_PersistsAnUpgradedHash()
        {
            var context = new FakeChatContext();
            var legacyHasher = new PasswordHasher<User>(new OptionsWrapperStub(new PasswordHasherOptions { IterationCount = 1 }));
            var user = User.Create("alice", "Alice");
            user.SetPasswordHash(legacyHasher.HashPassword(user, "correct horse battery staple"));
            var originalHash = user.PasswordHash;
            context.Seed(user);

            var currentHasher = new PasswordHasher<User>();
            var service = new UserService(context, currentHasher, new ChatChannelConfiguration());

            var loggedInUser = await service.LoginAsync("alice", "correct horse battery staple");

            Assert.NotEqual(originalHash, loggedInUser.PasswordHash);
            Assert.Equal(PasswordVerificationResult.Success, currentHasher.VerifyHashedPassword(loggedInUser, loggedInUser.PasswordHash, "correct horse battery staple"));
        }

        private sealed class OptionsWrapperStub(PasswordHasherOptions value) : Microsoft.Extensions.Options.IOptions<PasswordHasherOptions>
        {
            public PasswordHasherOptions Value { get; } = value;
        }
    }
}
