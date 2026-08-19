using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Chat.MongoDB;

namespace ThunderPropagator.UnitTests.Channels.Chat.MongoDB
{
    /// <summary>
    /// Issue #111: "DI registration validates required connection settings." These tests check
    /// validation and service registration only — they deliberately never resolve
    /// MongoDbChatContext, since doing so runs BaseChatContext's constructor, which calls
    /// Migrate() and would attempt a real connection to a MongoDB server this test suite doesn't
    /// have (see the class doc comment on MongoDbChatContext and the ticket discussion about
    /// scoping out live-server integration tests for now).
    /// </summary>
    public sealed class ChatMongoDbExtensionsTests
    {
        [Fact]
        public void AddChatChannel_ThrowsWhenConnectionStringIsMissing()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentException>(() =>
                services.AddChatChannel(settings => settings.DatabaseName = "chat"));
        }

        [Fact]
        public void AddChatChannel_ThrowsWhenDatabaseNameIsMissing()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentException>(() =>
                services.AddChatChannel(settings => settings.ConnectionString = "mongodb://localhost:27017"));
        }

        [Fact]
        public void AddChatChannel_RegistersMongoDbChatContext_WhenSettingsAreValid()
        {
            var services = new ServiceCollection();

            services.AddChatChannel(settings =>
            {
                settings.ConnectionString = "mongodb://localhost:27017";
                settings.DatabaseName = "chat";
            });

            Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(MongoDbChatContext));
        }
    }
}
