using System.Linq.Expressions;
using ThunderPropagator.Channels.Chat.Configuration;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models
{
    /// <summary>
    /// Issue #31: AddUserToGroupAsync, RemoveUserFromGroupAsync, RenameGroupAsync, and
    /// SetGroupIconAsync used to fetch the target group and mutate it with no check that the caller
    /// was a member — an IDOR letting any authenticated caller add/remove members, rename, or re-icon
    /// a group they don't belong to. Each now requires currentUserId to already be a member (via
    /// GetGroupDetailsAsync), except adding/removing yourself specifically (self-join/self-leave),
    /// which must keep working without requiring prior membership. There's no concrete IChatContext
    /// provider in this repo yet, so this uses a minimal in-memory fake, mirroring
    /// UserServiceAuthenticationTests' own FakeChatContext.
    /// </summary>
    public sealed class GroupServiceMembershipTests
    {
        private sealed class FakeChatContext : IChatContext
        {
            private readonly List<Group> _groups = [];

            public void Seed(Group group) => _groups.Add(group);

            public Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => Task.FromResult((TEntity?)(object?)_groups.SingleOrDefault(group => group.Id.Equals(id)));

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

        private static (GroupService Service, Group Group, Guid MemberId, Guid NonMemberId) CreateServiceWithGroup()
        {
            var context = new FakeChatContext();
            var configuration = new ChatChannelConfiguration();
            var service = new GroupService(context, configuration);

            var creatorId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var nonMemberId = Guid.NewGuid();

            var group = Group.Create("Test Group", creatorId).AddUser(memberId);
            context.Seed(group);

            return (service, group, memberId, nonMemberId);
        }

        [Fact]
        public async Task AddUserToGroupAsync_ByANonMember_ThrowsGroupAccessDeniedException()
        {
            var (service, group, _, nonMemberId) = CreateServiceWithGroup();

            await Assert.ThrowsAsync<GroupAccessDeniedException>(
                () => service.AddUserToGroupAsync(nonMemberId, group.Id, Guid.NewGuid()));
        }

        [Fact]
        public async Task AddUserToGroupAsync_ByAMember_Succeeds()
        {
            var (service, group, memberId, _) = CreateServiceWithGroup();
            var newUserId = Guid.NewGuid();

            var updatedGroup = await service.AddUserToGroupAsync(memberId, group.Id, newUserId);

            Assert.Contains(updatedGroup.GroupUsers, groupUser => groupUser.UserId == newUserId);
        }

        [Fact]
        public async Task AddUserToGroupAsync_SelfJoinByANonMember_Succeeds()
        {
            var (service, group, _, nonMemberId) = CreateServiceWithGroup();

            var updatedGroup = await service.AddUserToGroupAsync(nonMemberId, group.Id, nonMemberId);

            Assert.Contains(updatedGroup.GroupUsers, groupUser => groupUser.UserId == nonMemberId);
        }

        [Fact]
        public async Task RemoveUserFromGroupAsync_ByANonMember_ThrowsGroupAccessDeniedException()
        {
            var (service, group, memberId, nonMemberId) = CreateServiceWithGroup();

            await Assert.ThrowsAsync<GroupAccessDeniedException>(
                () => service.RemoveUserFromGroupAsync(nonMemberId, group.Id, memberId));
        }

        [Fact]
        public async Task RemoveUserFromGroupAsync_ByAMember_Succeeds()
        {
            var (service, group, memberId, _) = CreateServiceWithGroup();

            var updatedGroup = await service.RemoveUserFromGroupAsync(memberId, group.Id, memberId);

            Assert.DoesNotContain(updatedGroup.GroupUsers, groupUser => groupUser.UserId == memberId);
        }

        [Fact]
        public async Task RemoveUserFromGroupAsync_SelfLeaveByANonMember_DoesNotThrow()
        {
            var (service, group, _, nonMemberId) = CreateServiceWithGroup();

            var exception = await Record.ExceptionAsync(() => service.RemoveUserFromGroupAsync(nonMemberId, group.Id, nonMemberId));

            Assert.Null(exception);
        }

        [Fact]
        public async Task RenameGroupAsync_ByANonMember_ThrowsGroupAccessDeniedException()
        {
            var (service, group, _, nonMemberId) = CreateServiceWithGroup();

            await Assert.ThrowsAsync<GroupAccessDeniedException>(
                () => service.RenameGroupAsync(nonMemberId, group.Id, "New Name"));
        }

        [Fact]
        public async Task RenameGroupAsync_ByAMember_Succeeds()
        {
            var (service, group, memberId, _) = CreateServiceWithGroup();

            var updatedGroup = await service.RenameGroupAsync(memberId, group.Id, "New Name");

            Assert.Equal("New Name", updatedGroup.Name);
        }

        [Fact]
        public async Task SetGroupIconAsync_ByANonMember_ThrowsGroupAccessDeniedException()
        {
            var (service, group, _, nonMemberId) = CreateServiceWithGroup();

            await Assert.ThrowsAsync<GroupAccessDeniedException>(
                () => service.SetGroupIconAsync(nonMemberId, group.Id, "icon.png"));
        }

        [Fact]
        public async Task SetGroupIconAsync_ByAMember_Succeeds()
        {
            var (service, group, memberId, _) = CreateServiceWithGroup();

            var updatedGroup = await service.SetGroupIconAsync(memberId, group.Id, "icon.png");

            Assert.Equal("icon.png", updatedGroup.GroupIcon);
        }
    }
}
