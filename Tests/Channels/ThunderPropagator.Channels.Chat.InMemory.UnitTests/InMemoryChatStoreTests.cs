using ThunderPropagator.Channels.Chat.InMemory;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.InMemory
{
    /// <summary>
    /// Issue #112: "Concurrent CRUD and query operations do not corrupt state" and "Tests can create
    /// isolated stores and deterministic seed data."
    /// </summary>
    public sealed class InMemoryChatStoreTests
    {
        [Fact]
        public void Reset_ClearsEveryCollection()
        {
            var store = new InMemoryChatStore();
            var user = User.Create("reset-me", "Reset Me");
            user.SetPasswordHash("hash");
            store.Add(user);

            store.Reset();

            Assert.Empty(store.GetStore<User>().Values);
        }

        [Fact]
        public void Seed_InsertsEntitiesDirectly_ForDeterministicTestSetup()
        {
            var store = new InMemoryChatStore();
            var user = User.Create("seeded", "Seeded User");
            user.SetPasswordHash("hash");

            store.Seed(user);

            Assert.True(store.GetStore<User>().ContainsKey(user.Id));
        }

        [Fact]
        public void Seed_StillEnforcesUniqueness_SoBadTestDataFailsFast()
        {
            var store = new InMemoryChatStore();
            var first = User.Create("seed-dup", "First");
            first.SetPasswordHash("hash");
            var second = User.Create("seed-dup", "Second");
            second.SetPasswordHash("hash");
            store.Seed(first);

            Assert.Throws<InMemoryUniqueConstraintException>(() => store.Seed(second));
        }

        [Fact]
        public void TwoStores_AreFullyIsolatedFromEachOther()
        {
            var storeA = new InMemoryChatStore();
            var storeB = new InMemoryChatStore();
            var user = User.Create("isolated", "Isolated");
            user.SetPasswordHash("hash");

            storeA.Add(user);

            Assert.NotEmpty(storeA.GetStore<User>().Values);
            Assert.Empty(storeB.GetStore<User>().Values);
        }

        [Fact]
        public async Task ConcurrentAdds_WithTheSameUsername_OnlyOneSucceeds()
        {
            var store = new InMemoryChatStore();

            var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
            {
                var user = User.Create("race", "Racer");
                user.SetPasswordHash("hash");
                try
                {
                    store.Add(user);
                    return true;
                }
                catch (InMemoryUniqueConstraintException)
                {
                    return false;
                }
            }));

            var results = await Task.WhenAll(tasks);

            Assert.Single(results, succeeded => succeeded);
            Assert.Single(store.GetStore<User>().Values);
        }
    }
}
