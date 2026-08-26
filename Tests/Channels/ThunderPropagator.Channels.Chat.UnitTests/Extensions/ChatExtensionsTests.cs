using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Chat.Configuration;
using ThunderPropagator.Channels.Chat.Extensions;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.UnitTests.Extensions
{
    public class ChatExtensionsTests
    {
        private class DummyChatContext : BaseChatContext
        {
            protected override Task MigrateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            protected override Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public override Task<TEntity?> GetAsync<TEntity>(System.Linq.Expressions.Expression<System.Func<TEntity, bool>> expression, System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult<TEntity?>(null);
            public override Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult<TEntity?>(null);
            public override Task<System.Collections.Generic.IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(System.Linq.Expressions.Expression<System.Func<TEntity, bool>> expression, System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult((System.Collections.Generic.IReadOnlyCollection<TEntity>)new System.Collections.Generic.List<TEntity>());
            public override Task<System.Collections.Generic.IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult((System.Collections.Generic.IReadOnlyCollection<TEntity>)new System.Collections.Generic.List<TEntity>());
            public override Task<TEntity> CreateAsync<TEntity>(TEntity entity, System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult(entity);
            public override Task<TEntity> UpdateAsync<TEntity>(TEntity entity, System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult(entity);
            public override Task<bool> DeleteAsync<TEntity, TPk>(TPk id, System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult(true);
            public override Task<IReadOnlyCollection<User>> GetContactsAsync(Guid userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
            public override Task<MessageHistoryPage> GetDirectMessageHistoryAsync(Guid userId, Guid otherUserId, int page, int pageSize, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
            public override Task<MessageHistoryPage> GetGroupMessageHistoryAsync(Guid groupId, int page, int pageSize, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
            public override Task<UserSearchPage> SearchUsersAsync(string normalizedTerm, int page, int pageSize, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        [Fact]
        public void AddChatChannel_DoesNotThrow()
        {
            var services = new ServiceCollection();
            services.AddChatChannel<DummyChatContext>();
            Assert.NotNull(services);
        }

        // Issue #141: Validate() runs immediately after the consumer's channelConfigurator callback,
        // inside AddChatChannel itself, so an out-of-range value fails host startup rather than
        // surfacing later as a confusing runtime failure.
        [Fact]
        public void AddChatChannel_WithAnInvalidMaxMessageLength_ThrowsArgumentOutOfRangeException()
        {
            var services = new ServiceCollection();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                services.AddChatChannel<DummyChatContext>(configuration => configuration.MaxMessageLength = 0));

            Assert.Equal(nameof(ChatChannelConfiguration.MaxMessageLength), exception.ParamName);
        }

        [Fact]
        public void AddChatChannel_WithAnInvalidMessageHistoryPageSize_ThrowsArgumentOutOfRangeException()
        {
            var services = new ServiceCollection();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                services.AddChatChannel<DummyChatContext>(configuration => configuration.MessageHistoryPageSize = 0));

            Assert.Equal(nameof(ChatChannelConfiguration.MessageHistoryPageSize), exception.ParamName);
        }

        [Fact]
        public void AddChatChannel_WithValidChatSpecificLimits_DoesNotThrow()
        {
            var services = new ServiceCollection();

            services.AddChatChannel<DummyChatContext>(configuration =>
            {
                configuration.MaxMessageLength = 500;
                configuration.MessageHistoryPageSize = 25;
                configuration.AllowGuestRegister = false;
            });

            Assert.NotNull(services);
        }
    }
}
