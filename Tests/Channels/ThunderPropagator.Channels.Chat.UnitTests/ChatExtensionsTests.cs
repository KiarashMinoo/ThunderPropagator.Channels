using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.UnitTests
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
        }

        [Fact]
        public void AddChatChannel_DoesNotThrow()
        {
            var services = new ServiceCollection();
            services.AddChatChannel<DummyChatContext>();
            Assert.NotNull(services);
        }
    }
}
