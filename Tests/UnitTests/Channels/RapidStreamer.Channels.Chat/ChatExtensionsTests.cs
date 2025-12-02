using Microsoft.Extensions.DependencyInjection;
using Xunit;
using RapidStreamer.Channels.Chat.Models;

namespace RapidStreamer.UnitTests.Channels.Chat
{
    public class ChatExtensionsTests
    {
        private class DummyChatContext : BaseChatContext
        {
            protected override void Migrate() { }
            protected override void Seed() { }
            public override Task<TEntity?> GetAsync<TEntity>(System.Linq.Expressions.Expression<System.Func<TEntity, bool>> expression, System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult<TEntity?>(null);
            public override Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult<TEntity?>(null);
            public override Task<System.Collections.Generic.IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(System.Linq.Expressions.Expression<System.Func<TEntity, bool>> expression, System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult((System.Collections.Generic.IReadOnlyCollection<TEntity>)new System.Collections.Generic.List<TEntity>());
            public override Task<System.Collections.Generic.IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult((System.Collections.Generic.IReadOnlyCollection<TEntity>)new System.Collections.Generic.List<TEntity>());
            public override Task<TEntity> CreateAsync<TEntity>(TEntity entity, System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult(entity);
            public override Task<TEntity> UpdateAsync<TEntity>(TEntity entity, System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult(entity);
            public override Task<bool> DeleteAsync<TEntity, TPk>(TPk id, System.Threading.CancellationToken cancellationToken = default) where TEntity : class => Task.FromResult(true);
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
