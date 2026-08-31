using System.Linq.Expressions;
using ThunderPropagator.Channels.Chat.Configuration;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models
{
    /// <summary>
    /// Issue #38: CreateAsync/RenameGroupAsync/SetGroupIconAsync used to accept a group Name/Icon of
    /// any length — the same unbounded-input gap MaxMessageLength (#141) already closed for message
    /// Body. There's no concrete IChatContext provider in this repo yet, so this uses a minimal
    /// in-memory fake, mirroring GroupServiceMembershipTests' own FakeChatContext.
    /// </summary>
    public sealed class GroupServiceValidationTests
    {
        private sealed class FakeChatContext : IChatContext
        {
            private readonly List<Group> _groups = [];
            private readonly List<User> _users = [];

            public void Seed(Group group) => _groups.Add(group);
            public void Seed(User user) => _users.Add(user);

            public Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
            {
                if (typeof(TEntity) == typeof(User))
                    return Task.FromResult((TEntity?)(object?)_users.SingleOrDefault(user => user.Id.Equals(id)));

                return Task.FromResult((TEntity?)(object?)_groups.SingleOrDefault(group => group.Id.Equals(id)));
            }

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
            {
                _groups.Add((Group)(object)entity!);
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

        private static (GroupService Service, ChatChannelConfiguration Configuration, Group Group, Guid MemberId) CreateServiceWithGroup()
        {
            var context = new FakeChatContext();
            var configuration = new ChatChannelConfiguration();
            var service = new GroupService(context, configuration);

            var creatorId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var group = Group.Create("Test Group", creatorId).AddUser(memberId);
            context.Seed(group);

            return (service, configuration, group, memberId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateAsync_WithAnEmptyName_ThrowsInvalidGroupCreateRequestException(string name)
        {
            var context = new FakeChatContext();
            var service = new GroupService(context, new ChatChannelConfiguration());

            await Assert.ThrowsAsync<InvalidGroupCreateRequestException>(
                () => service.CreateAsync(name, Guid.NewGuid(), []));
        }

        [Fact]
        public async Task CreateAsync_WithANameOverMaxGroupNameLength_ThrowsInvalidGroupCreateRequestException()
        {
            var context = new FakeChatContext();
            var configuration = new ChatChannelConfiguration { MaxGroupNameLength = 10 };
            var service = new GroupService(context, configuration);

            await Assert.ThrowsAsync<InvalidGroupCreateRequestException>(
                () => service.CreateAsync(new string('a', 11), Guid.NewGuid(), []));
        }

        [Fact]
        public async Task CreateAsync_WithANameAtMaxGroupNameLength_Succeeds()
        {
            var context = new FakeChatContext();
            var configuration = new ChatChannelConfiguration { MaxGroupNameLength = 10 };
            var service = new GroupService(context, configuration);

            var group = await service.CreateAsync(new string('a', 10), Guid.NewGuid(), []);

            Assert.Equal(10, group.Name.Length);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RenameGroupAsync_WithAnEmptyName_ThrowsInvalidGroupCreateRequestException(string name)
        {
            var (service, _, group, memberId) = CreateServiceWithGroup();

            await Assert.ThrowsAsync<InvalidGroupCreateRequestException>(
                () => service.RenameGroupAsync(memberId, group.Id, name));
        }

        [Fact]
        public async Task RenameGroupAsync_WithANameOverMaxGroupNameLength_ThrowsInvalidGroupCreateRequestException()
        {
            var (service, configuration, group, memberId) = CreateServiceWithGroup();
            configuration.MaxGroupNameLength = 10;

            await Assert.ThrowsAsync<InvalidGroupCreateRequestException>(
                () => service.RenameGroupAsync(memberId, group.Id, new string('a', 11)));
        }

        [Fact]
        public async Task SetGroupIconAsync_WithAnIconOverMaxGroupIconLength_ThrowsInvalidGroupIconRequestException()
        {
            var (service, configuration, group, memberId) = CreateServiceWithGroup();
            configuration.MaxGroupIconLength = 10;

            await Assert.ThrowsAsync<InvalidGroupIconRequestException>(
                () => service.SetGroupIconAsync(memberId, group.Id, new string('a', 11)));
        }

        [Fact]
        public async Task SetGroupIconAsync_WithAnEmptyIcon_Succeeds()
        {
            var (service, _, group, memberId) = CreateServiceWithGroup();

            var updatedGroup = await service.SetGroupIconAsync(memberId, group.Id, string.Empty);

            Assert.Equal(string.Empty, updatedGroup.GroupIcon);
        }
    }
}
