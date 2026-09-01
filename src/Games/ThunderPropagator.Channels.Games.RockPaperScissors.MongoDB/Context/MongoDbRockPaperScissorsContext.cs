using MongoDB.Driver;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;
using ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB.Serialization;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB.Context
{
    /// <summary>
    /// Implements the RockPaperScissors persistence contract on top of the MongoDB C# driver — mirrors
    /// ThunderPropagator.Channels.Chat.MongoDB's own MongoDbChatContext. Simpler than that one: both
    /// entities here use their own natural string key (ConnectionId/SessionId) as Mongo's own <c>_id</c>,
    /// so no separate unique index needs creating — <c>_id</c> uniqueness is what
    /// <see cref="TryReserveConnectionAsync"/> relies on, and neither entity has any navigation
    /// property to populate after a read.
    /// </summary>
    public sealed class MongoDbRockPaperScissorsContext : BaseRockPaperScissorsContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbRockPaperScissorsContext(IMongoDatabase database)
        {
            RockPaperScissorsBsonSerializers.EnsureRegistered();
            _database = database;
        }

        // Nothing to migrate — no indexes beyond the implicit unique _id every collection already has.
        protected override Task MigrateAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        // No default seed data.
        protected override Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static string GetCollectionName<TEntity>()
        {
            if (typeof(TEntity) == typeof(RockPaperScissorsMatchReservation)) return "RockPaperScissorsMatchReservations";
            if (typeof(TEntity) == typeof(RockPaperScissorsGameSessionRecord)) return "RockPaperScissorsGameSessionRecords";

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

        /// <summary>
        /// ConnectionId is <see cref="RockPaperScissorsMatchReservation"/>'s own Mongo <c>_id</c>, so a
        /// second insert for an already-reserved connectionId fails Mongo's own unique-index-on-_id
        /// constraint — caught here (rather than a generic CreateAsync a caller would need to wrap in
        /// its own try/catch) and turned into a false return.
        /// </summary>
        public override async Task<bool> TryReserveConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
        {
            try
            {
                await GetCollection<RockPaperScissorsMatchReservation>()
                    .InsertOneAsync(RockPaperScissorsMatchReservation.Create(connectionId), cancellationToken: cancellationToken);
                return true;
            }
            catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }
    }
}
