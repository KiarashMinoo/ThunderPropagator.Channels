using System.Linq.Expressions;
using ThunderPropagator.Channels.Chat.Models;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models
{
    /// <summary>
    /// Issue #113: BaseChatContext used to guard Migrate()/Seed() with a single static bool shared
    /// by every subclass — once any one concrete provider type initialized, every other provider
    /// type would see it already true and skip its own Migrate()/Seed() entirely. Initialization
    /// state is now keyed per concrete type, with its own lock, so unrelated provider types can
    /// never interfere with each other's initialization.
    ///
    /// Each test below uses its own private nested subclass rather than sharing one: the
    /// per-type state is itself static, so reusing a type across tests would leak initialization
    /// state between them the same way the original bug leaked it between provider types.
    /// </summary>
    public sealed class BaseChatContextInitializationTests
    {
        private abstract class RecordingChatContext(Action onMigrate, Action onSeed) : BaseChatContext
        {
            protected override void Migrate() => onMigrate();
            protected override void Seed() => onSeed();

            public override Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public override Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public override Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public override Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public override Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public override Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public override Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();
        }

        private sealed class ProviderAContext(Action onMigrate, Action onSeed) : RecordingChatContext(onMigrate, onSeed);
        private sealed class ProviderBContext(Action onMigrate, Action onSeed) : RecordingChatContext(onMigrate, onSeed);
        private sealed class RepeatConstructionContext(Action onMigrate, Action onSeed) : RecordingChatContext(onMigrate, onSeed);
        private sealed class ConcurrentConstructionContext(Action onMigrate, Action onSeed) : RecordingChatContext(onMigrate, onSeed);
        private sealed class FailThenRetryContext(Action onMigrate, Action onSeed) : RecordingChatContext(onMigrate, onSeed);
        private sealed class FailureDoesNotRunSeedContext(Action onMigrate, Action onSeed) : RecordingChatContext(onMigrate, onSeed);

        [Fact]
        public void TwoDifferentProviderTypes_EachInitializeIndependently()
        {
            var migrateCallsA = 0;
            var migrateCallsB = 0;

            _ = new ProviderAContext(() => migrateCallsA++, () => { });
            _ = new ProviderBContext(() => migrateCallsB++, () => { });

            Assert.Equal(1, migrateCallsA);
            Assert.Equal(1, migrateCallsB);
        }

        [Fact]
        public void ConstructingTheSameProviderTypeAgain_DoesNotReinitialize()
        {
            var migrateCalls = 0;
            var seedCalls = 0;

            _ = new RepeatConstructionContext(() => migrateCalls++, () => seedCalls++);
            _ = new RepeatConstructionContext(() => migrateCalls++, () => seedCalls++);
            _ = new RepeatConstructionContext(() => migrateCalls++, () => seedCalls++);

            Assert.Equal(1, migrateCalls);
            Assert.Equal(1, seedCalls);
        }

        [Fact]
        public async Task ConcurrentConstruction_OfTheSameProviderType_InitializesExactlyOnce()
        {
            var migrateCalls = 0;

            var tasks = Enumerable.Range(0, 50)
                .Select(_ => Task.Run(() => (BaseChatContext)new ConcurrentConstructionContext(
                    () => Interlocked.Increment(ref migrateCalls), () => { })))
                .ToArray();
            await Task.WhenAll(tasks);

            Assert.Equal(1, migrateCalls);
        }

        [Fact]
        public void FailedMigration_DoesNotMarkTheProviderAsInitialized_AndTheNextAttemptRetries()
        {
            var attempts = 0;

            void Migrate()
            {
                attempts++;
                if (attempts == 1)
                    throw new InvalidOperationException("first attempt fails");
            }

            Assert.Throws<InvalidOperationException>(() => new FailThenRetryContext(Migrate, () => { }));
            // Per the documented retry policy: the next construction of the same concrete type
            // retries Migrate() from scratch, since the failed attempt never marked it initialized.
            _ = new FailThenRetryContext(Migrate, () => { });

            Assert.Equal(2, attempts);
        }

        [Fact]
        public void FailedMigration_NeverRunsSeed()
        {
            var seedCalls = 0;

            Assert.Throws<InvalidOperationException>(() => new FailureDoesNotRunSeedContext(
                () => throw new InvalidOperationException("migration fails"),
                () => seedCalls++));

            Assert.Equal(0, seedCalls);
        }
    }
}
