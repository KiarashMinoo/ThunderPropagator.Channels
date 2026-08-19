using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.EntityFrameworkCore
{
    /// <summary>
    /// Implements the Chat channel's persistence contract (<see cref="BaseChatContext"/>) on top of
    /// <see cref="ChatDbContext"/>. Registered as Scoped alongside the channel's other services (see
    /// <see cref="ChatEntityFrameworkCoreExtensions"/>), so within one scope an entity returned by
    /// <see cref="GetAsync{TEntity}"/>/<see cref="GetAllAsync{TEntity}(CancellationToken)"/> is always
    /// already tracked by <see cref="ChatDbContext"/> by the time it comes back through
    /// <see cref="UpdateAsync{TEntity}"/> — see the comment there for why that matters.
    /// </summary>
    public sealed class EntityFrameworkCoreChatContext(ChatDbContext dbContext) : BaseChatContext
    {
        protected override Task MigrateAsync(CancellationToken cancellationToken) => dbContext.Database.MigrateAsync(cancellationToken);

        // No default seed data — the Chat domain has no fixed reference data to install.
        protected override Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override async Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
            => await dbContext.Set<TEntity>().FirstOrDefaultAsync(expression, cancellationToken);

        public override async Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
            => await dbContext.Set<TEntity>().FindAsync([id], cancellationToken);

        public override async Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
            => await dbContext.Set<TEntity>().Where(expression).ToListAsync(cancellationToken);

        public override async Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
            => await dbContext.Set<TEntity>().ToListAsync(cancellationToken);

        public override async Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            dbContext.Set<TEntity>().Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public override async Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            // Every caller in this codebase fetches the entity through this same context and then
            // mutates it in place (e.g. Group.AddUser appends to the tracked GroupUsers collection)
            // before calling UpdateAsync, so it's already tracked here. Calling Set<TEntity>().Update()
            // unconditionally would re-walk the whole navigation graph and, because GroupUser/Message
            // use client-generated non-default Guid keys, EF can't tell a freshly-added child apart
            // from an existing one — Update() would mark it Modified instead of Added and the insert
            // would silently vanish. Only attach when the entity truly isn't tracked yet; otherwise
            // just save and let change tracking (already watching the mutated graph) do the right thing.
            if (dbContext.Entry(entity).State == EntityState.Detached)
                dbContext.Set<TEntity>().Update(entity);

            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public override async Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
        {
            var entity = await dbContext.Set<TEntity>().FindAsync([id], cancellationToken);
            if (entity is null)
                return false;

            dbContext.Set<TEntity>().Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        // Issue #115: a single query, composed entirely server-side — the inner IQueryable<Guid> is
        // never materialized on its own, so EF translates this into one SQL statement with an IN
        // subquery. No Message row (let alone its Body) is ever loaded into memory, and neither
        // Message.Sender/Receiver nor User's own navigations need to be populated for this to work.
        public override async Task<IReadOnlyCollection<User>> GetContactsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var otherUserIds = dbContext.Set<Message>()
                .Where(message => message.SenderId == userId || message.ReceiverId == userId)
                .Select(message => message.SenderId == userId ? message.ReceiverId : message.SenderId)
                .Distinct();

            return await dbContext.Set<User>()
                .Where(user => otherUserIds.Contains(user.Id))
                .ToListAsync(cancellationToken);
        }
    }
}
