using System.Linq.Expressions;
using ThunderPropagator.Channels.Chat.Configuration;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models
{
    /// <summary>
    /// Issue #38: SendMessageAsync's ValidateBodyLength never rejected a null/blank Body, despite
    /// EditMessageAsync's own "same rules as new messages" comment implying it already matched that
    /// baseline. There's no concrete IChatContext provider in this repo yet, so this uses a minimal
    /// in-memory fake, mirroring MessageServiceMembershipTests' own FakeChatContext.
    /// </summary>
    public sealed class MessageServiceValidationTests
    {
        private sealed class FakeChatContext : IChatContext
        {
            public Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
                => Task.FromResult(entity);

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

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendMessageAsync_WithABlankBody_ThrowsInvalidMessageSendException(string body)
        {
            var service = new MessageService(new FakeChatContext(), new ChatChannelConfiguration());

            await Assert.ThrowsAsync<InvalidMessageSendException>(
                () => service.SendMessageAsync(Guid.NewGuid(), Guid.NewGuid(), body));
        }

        [Fact]
        public async Task SendMessageAsync_WithANonBlankBody_Succeeds()
        {
            var service = new MessageService(new FakeChatContext(), new ChatChannelConfiguration());

            var message = await service.SendMessageAsync(Guid.NewGuid(), Guid.NewGuid(), "hello");

            Assert.Equal("hello", message.Body);
        }
    }
}
