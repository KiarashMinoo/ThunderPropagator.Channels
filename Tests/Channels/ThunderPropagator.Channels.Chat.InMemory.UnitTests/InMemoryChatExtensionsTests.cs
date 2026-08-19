using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Chat.InMemory;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.InMemory
{
    /// <summary>
    /// Unlike #110/#111's DI registration tests, InMemoryChatContext has no live external resource to
    /// avoid touching — resolving and actually exercising it through DI is safe, so these tests do
    /// both, plus confirm the store singleton is what makes data survive across separate resolutions
    /// of the scoped context.
    /// </summary>
    public sealed class InMemoryChatExtensionsTests
    {
        [Fact]
        public void AddChatChannel_RegistersInMemoryChatContextAndStore()
        {
            var services = new ServiceCollection();

            services.AddChatChannel();

            var serviceProvider = services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<InMemoryChatContext>();
            var store = serviceProvider.GetRequiredService<InMemoryChatStore>();

            Assert.NotNull(context);
            Assert.NotNull(store);
        }

        [Fact]
        public async Task DataCreatedInOneScope_IsVisibleInAnotherScope()
        {
            var services = new ServiceCollection();
            services.AddChatChannel();
            var serviceProvider = services.BuildServiceProvider();

            Guid userId;
            using (var firstScope = serviceProvider.CreateScope())
            {
                var context = firstScope.ServiceProvider.GetRequiredService<InMemoryChatContext>();
                var user = User.Create("scoped", "Scoped User");
                user.SetPasswordHash("hash");
                await context.CreateAsync(user, CancellationToken.None);
                userId = user.Id;
            }

            using var secondScope = serviceProvider.CreateScope();
            var secondContext = secondScope.ServiceProvider.GetRequiredService<InMemoryChatContext>();
            var reloaded = await secondContext.GetAsync<User, Guid>(userId, CancellationToken.None);

            Assert.NotNull(reloaded);
            Assert.Equal("scoped", reloaded.UserName);
        }
    }
}
