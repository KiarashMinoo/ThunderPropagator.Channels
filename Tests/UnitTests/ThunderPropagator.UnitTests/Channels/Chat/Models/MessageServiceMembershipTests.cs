using System.Linq.Expressions;
using ThunderPropagator.Channels.Chat.Configuration;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models
{
    /// <summary>
    /// Issue #33: SendMessageToGroupAsync fanned a message out to every member of any group the
    /// caller named, with no check that senderId was actually one of them — any authenticated user
    /// could send into a group they don't belong to just by knowing its GroupId. Now requires senderId
    /// to be a current member (the same check GetGroupMessageHistoryAsync already had). There's no
    /// concrete IChatContext provider in this repo yet, so this uses a minimal in-memory fake,
    /// mirroring UserServiceAuthenticationTests/GroupServiceMembershipTests' own FakeChatContext.
    /// </summary>
    public sealed class MessageServiceMembershipTests
    {
        private sealed class FakeChatContext : IChatContext
        {
            private readonly List<Group> _groups = [];
            private readonly List<Message> _messages = [];

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
                _messages.Add((Message)(object)entity!);
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

        private static (MessageService Service, Group Group, Guid MemberId, Guid NonMemberId) CreateServiceWithGroup()
        {
            var context = new FakeChatContext();
            var service = new MessageService(context, new ChatChannelConfiguration());

            var creatorId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var nonMemberId = Guid.NewGuid();

            var group = Group.Create("Test Group", creatorId).AddUser(memberId);
            context.Seed(group);

            return (service, group, memberId, nonMemberId);
        }

        [Fact]
        public async Task SendMessageToGroupAsync_ByANonMember_ThrowsGroupAccessDeniedException()
        {
            var (service, group, _, nonMemberId) = CreateServiceWithGroup();

            await Assert.ThrowsAsync<GroupAccessDeniedException>(
                () => service.SendMessageToGroupAsync(nonMemberId, group.Id, "sneaky"));
        }

        [Fact]
        public async Task SendMessageToGroupAsync_ByAMember_DeliversNormally()
        {
            var (service, group, memberId, _) = CreateServiceWithGroup();

            var sent = await service.SendMessageToGroupAsync(memberId, group.Id, "hello group");

            Assert.Single(sent);
            Assert.Contains(sent, message => message.ReceiverId == memberId);
        }

        [Fact]
        public async Task SendMessageAsync_DirectMessage_IsUnaffectedByGroupMembership()
        {
            var (service, _, _, nonMemberId) = CreateServiceWithGroup();
            var receiverId = Guid.NewGuid();

            var message = await service.SendMessageAsync(nonMemberId, receiverId, "hi");

            Assert.Equal(nonMemberId, message.SenderId);
            Assert.Equal(receiverId, message.ReceiverId);
        }
    }
}
