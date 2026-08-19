using System.Linq.Expressions;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models
{
    /// <summary>
    /// Issue #113: BaseChatContext used to guard Migrate()/Seed() with a single static bool shared
    /// by every subclass — once any one concrete provider type initialized, every other provider
    /// type would see it already true and skip its own Migrate()/Seed() entirely. Initialization
    /// state is keyed per concrete type instead, so unrelated provider types can never interfere
    /// with each other's initialization.
    ///
    /// Issue #114: Migrate()/Seed() moved off the constructor entirely and onto an explicit, awaited,
    /// cancellable InitializeAsync() — these tests now construct a context and call InitializeAsync()
    /// as two separate steps, and additionally cover cancellation and ordering per that ticket's AC.
    ///
    /// Each test below uses its own private nested subclass rather than sharing one: the per-type
    /// state is itself static, so reusing a type across tests would leak initialization state between
    /// them the same way the original #113 bug leaked it between provider types.
    /// </summary>
    public sealed class BaseChatContextInitializationTests
    {
        private abstract class RecordingChatContext(Func<CancellationToken, Task> onMigrate, Func<CancellationToken, Task> onSeed) : BaseChatContext
        {
            protected override Task MigrateAsync(CancellationToken cancellationToken) => onMigrate(cancellationToken);
            protected override Task SeedAsync(CancellationToken cancellationToken) => onSeed(cancellationToken);

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

            public override Task<IReadOnlyCollection<User>> GetContactsAsync(Guid userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        private static Func<CancellationToken, Task> Sync(Action action) => _ =>
        {
            action();
            return Task.CompletedTask;
        };

        private sealed class ProviderAContext(Func<CancellationToken, Task> onMigrate, Func<CancellationToken, Task> onSeed) : RecordingChatContext(onMigrate, onSeed);
        private sealed class ProviderBContext(Func<CancellationToken, Task> onMigrate, Func<CancellationToken, Task> onSeed) : RecordingChatContext(onMigrate, onSeed);
        private sealed class RepeatInitializationContext(Func<CancellationToken, Task> onMigrate, Func<CancellationToken, Task> onSeed) : RecordingChatContext(onMigrate, onSeed);
        private sealed class ConcurrentInitializationContext(Func<CancellationToken, Task> onMigrate, Func<CancellationToken, Task> onSeed) : RecordingChatContext(onMigrate, onSeed);
        private sealed class FailThenRetryContext(Func<CancellationToken, Task> onMigrate, Func<CancellationToken, Task> onSeed) : RecordingChatContext(onMigrate, onSeed);
        private sealed class FailureDoesNotRunSeedContext(Func<CancellationToken, Task> onMigrate, Func<CancellationToken, Task> onSeed) : RecordingChatContext(onMigrate, onSeed);
        private sealed class OrderingContext(Func<CancellationToken, Task> onMigrate, Func<CancellationToken, Task> onSeed) : RecordingChatContext(onMigrate, onSeed);
        private sealed class AlreadyCancelledContext(Func<CancellationToken, Task> onMigrate, Func<CancellationToken, Task> onSeed) : RecordingChatContext(onMigrate, onSeed);
        private sealed class CancelledDuringMigrateContext(Func<CancellationToken, Task> onMigrate, Func<CancellationToken, Task> onSeed) : RecordingChatContext(onMigrate, onSeed);

        [Fact]
        public async Task TwoDifferentProviderTypes_EachInitializeIndependently()
        {
            var migrateCallsA = 0;
            var migrateCallsB = 0;

            await new ProviderAContext(Sync(() => migrateCallsA++), Sync(() => { })).InitializeAsync();
            await new ProviderBContext(Sync(() => migrateCallsB++), Sync(() => { })).InitializeAsync();

            Assert.Equal(1, migrateCallsA);
            Assert.Equal(1, migrateCallsB);
        }

        [Fact]
        public async Task CallingInitializeAsyncAgain_OnTheSameProviderType_DoesNotReinitialize()
        {
            var migrateCalls = 0;
            var seedCalls = 0;

            await new RepeatInitializationContext(Sync(() => migrateCalls++), Sync(() => seedCalls++)).InitializeAsync();
            await new RepeatInitializationContext(Sync(() => migrateCalls++), Sync(() => seedCalls++)).InitializeAsync();
            await new RepeatInitializationContext(Sync(() => migrateCalls++), Sync(() => seedCalls++)).InitializeAsync();

            Assert.Equal(1, migrateCalls);
            Assert.Equal(1, seedCalls);
        }

        [Fact]
        public async Task ConcurrentInitializeAsyncCalls_OfTheSameProviderType_InitializeExactlyOnce()
        {
            var migrateCalls = 0;

            var tasks = Enumerable.Range(0, 50)
                .Select(_ => new ConcurrentInitializationContext(
                    Sync(() => Interlocked.Increment(ref migrateCalls)), Sync(() => { })).InitializeAsync())
                .ToArray();
            await Task.WhenAll(tasks);

            Assert.Equal(1, migrateCalls);
        }

        [Fact]
        public async Task MigrateThenSeed_RunInOrder()
        {
            var order = new List<string>();

            await new OrderingContext(
                Sync(() => order.Add("Migrate")),
                Sync(() => order.Add("Seed"))).InitializeAsync();

            Assert.Equal(["Migrate", "Seed"], order);
        }

        [Fact]
        public async Task FailedMigration_DoesNotMarkTheProviderAsInitialized_AndTheNextAttemptRetries()
        {
            var attempts = 0;

            Task Migrate(CancellationToken _)
            {
                attempts++;
                if (attempts == 1)
                    throw new InvalidOperationException("first attempt fails");
                return Task.CompletedTask;
            }

            await Assert.ThrowsAsync<InvalidOperationException>(() => new FailThenRetryContext(Migrate, Sync(() => { })).InitializeAsync());
            // Per the documented retry policy: the next InitializeAsync() call for the same concrete
            // type retries MigrateAsync() from scratch, since the failed attempt never marked it
            // initialized.
            await new FailThenRetryContext(Migrate, Sync(() => { })).InitializeAsync();

            Assert.Equal(2, attempts);
        }

        [Fact]
        public async Task FailedMigration_NeverRunsSeed()
        {
            var seedCalls = 0;

            await Assert.ThrowsAsync<InvalidOperationException>(() => new FailureDoesNotRunSeedContext(
                _ => throw new InvalidOperationException("migration fails"),
                Sync(() => seedCalls++)).InitializeAsync());

            Assert.Equal(0, seedCalls);
        }

        [Fact]
        public async Task InitializeAsync_WithAnAlreadyCancelledToken_ThrowsWithoutRunningMigrateOrSeed()
        {
            var migrateCalls = 0;
            var seedCalls = 0;
            var context = new AlreadyCancelledContext(Sync(() => migrateCalls++), Sync(() => seedCalls++));
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            // SemaphoreSlim.WaitAsync throws the OperationCanceledException subclass TaskCanceledException
            // for an already-cancelled token; ThrowsAnyAsync matches either since both signal cancellation.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.InitializeAsync(cts.Token));

            Assert.Equal(0, migrateCalls);
            Assert.Equal(0, seedCalls);
        }

        [Fact]
        public async Task InitializeAsync_CancelledDuringMigrate_DoesNotMarkTheProviderAsInitialized_AndASubsequentCallSucceeds()
        {
            var attempts = 0;
            using var cts = new CancellationTokenSource();

            Task Migrate(CancellationToken cancellationToken)
            {
                attempts++;
                if (attempts == 1)
                {
                    cts.Cancel();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return Task.CompletedTask;
            }

            var first = new CancelledDuringMigrateContext(Migrate, Sync(() => { }));
            await Assert.ThrowsAsync<OperationCanceledException>(() => first.InitializeAsync(cts.Token));

            var second = new CancelledDuringMigrateContext(Migrate, Sync(() => { }));
            await second.InitializeAsync();

            Assert.Equal(2, attempts);
        }
    }
}
