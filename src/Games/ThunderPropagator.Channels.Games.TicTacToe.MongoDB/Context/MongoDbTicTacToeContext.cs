using MongoDB.Driver;
using ThunderPropagator.Channels.Games.TicTacToe.Models;
using ThunderPropagator.Channels.Games.TicTacToe.MongoDB.Serialization;

namespace ThunderPropagator.Channels.Games.TicTacToe.MongoDB.Context
{
    /// <summary>
    /// Implements the TicTacToe persistence contract on top of the MongoDB C# driver — mirrors
    /// ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB's own MongoDbRockPaperScissorsContext.
    /// SessionId is its own natural Mongo <c>_id</c>, so no separate unique index needs creating.
    /// </summary>
    public sealed class MongoDbTicTacToeContext : BaseTicTacToeContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbTicTacToeContext(IMongoDatabase database)
        {
            TicTacToeBsonSerializers.EnsureRegistered();
            _database = database;
        }

        // Nothing to migrate — no indexes beyond the implicit unique _id every collection already has.
        protected override Task MigrateAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        // No default seed data.
        protected override Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static string GetCollectionName<TEntity>()
        {
            if (typeof(TEntity) == typeof(TicTacToeGameRecord)) return "TicTacToeGames";

            throw new NotSupportedException($"No collection mapping for {typeof(TEntity).Name}.");
        }

        private IMongoCollection<TEntity> GetCollection<TEntity>() where TEntity : class
            => _database.GetCollection<TEntity>(GetCollectionName<TEntity>());

        public override async Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
        {
            var filter = Builders<TEntity>.Filter.Eq("_id", id);
            return await GetCollection<TEntity>().Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
            => await GetCollection<TEntity>().Find(FilterDefinition<TEntity>.Empty).ToListAsync(cancellationToken);

        public override async Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            await GetCollection<TEntity>().InsertOneAsync(entity, cancellationToken: cancellationToken);
            return entity;
        }

        public override async Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            var id = entity switch
            {
                TicTacToeGameRecord game => (object)game.SessionId,
                _ => throw new NotSupportedException($"No id accessor for {typeof(TEntity).Name}.")
            };

            var filter = Builders<TEntity>.Filter.Eq("_id", id);
            await GetCollection<TEntity>().ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true }, cancellationToken);
            return entity;
        }

        public override async Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
        {
            var filter = Builders<TEntity>.Filter.Eq("_id", id);
            var result = await GetCollection<TEntity>().DeleteOneAsync(filter, cancellationToken);
            return result.DeletedCount > 0;
        }
    }
}
