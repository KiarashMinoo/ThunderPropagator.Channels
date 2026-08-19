using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Chat.EntityFrameworkCore;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.EntityFrameworkCore
{
    /// <summary>
    /// Issue #110: integration tests for ThunderPropagator.Channels.Chat.EntityFrameworkCore, run
    /// against a real SQLite database (a production-style container is out of scope for now — see
    /// the ticket discussion). These exercise UserService/GroupService/MessageService directly
    /// against EntityFrameworkCoreChatContext, the same way the Chat channel's own pipelines do,
    /// rather than re-implementing that logic here — internals visibility to this assembly is
    /// granted the same way it already is to ThunderPropagator.UnitTests.
    /// </summary>
    public sealed class ChatEntityFrameworkCoreIntegrationTests(ChatDatabaseFixture fixture) : IClassFixture<ChatDatabaseFixture>
    {
        private static (UserService Users, GroupService Groups, MessageService Messages, ChatDbContext DbContext) CreateServices(ChatDatabaseFixture fixture)
        {
            var dbContext = fixture.CreateDbContext();
            var chatContext = new EntityFrameworkCoreChatContext(dbContext);
            var passwordHasher = new PasswordHasher<User>();

            return (new UserService(chatContext, passwordHasher), new GroupService(chatContext), new MessageService(chatContext), dbContext);
        }

        [Fact]
        public void Migrate_AppliesTheScaffoldedSqliteMigration()
        {
            var (_, _, _, dbContext) = CreateServices(fixture);

            var applied = dbContext.Database.GetAppliedMigrations();

            Assert.Contains(applied, migrationId => migrationId.EndsWith("_InitialCreate", StringComparison.Ordinal));
        }

        [Fact]
        public async Task RegisterThenLogin_RoundTripsAUserThroughSqlite()
        {
            var (users, _, _, _) = CreateServices(fixture);
            var username = $"alice-{Guid.NewGuid():N}";

            await users.RegisterAsync(username, "correct horse battery staple", "Alice", CancellationToken.None);
            var loggedIn = await users.LoginAsync(username, "correct horse battery staple", CancellationToken.None);

            Assert.Equal(username, loggedIn.UserName);
            Assert.Equal("Alice", loggedIn.Name);
        }

        [Fact]
        public async Task RegisteringTheSameUsernameTwice_ViolatesTheUniqueIndex()
        {
            var (users, _, _, dbContext) = CreateServices(fixture);
            var username = $"dup-{Guid.NewGuid():N}";
            await users.RegisterAsync(username, "password-one", "First", CancellationToken.None);

            // UserService.RegisterAsync already checks for an existing username itself — this
            // bypasses that in-app check to prove the database-level constraint from
            // UserConfiguration is what actually stops a duplicate from ever being persisted, not
            // just the service's own pre-check.
            var duplicate = User.Create(username, "Second");
            duplicate.SetPasswordHash("irrelevant");
            dbContext.Add(duplicate);

            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync(CancellationToken.None));
        }

        [Fact]
        public async Task GroupUsers_IsPopulatedAfterCreate_SoSendMessageToGroupReachesEveryMember()
        {
            var (users, groups, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var memberA = await users.RegisterAsync($"member-a-{Guid.NewGuid():N}", "password", "MemberA", CancellationToken.None);
            var memberB = await users.RegisterAsync($"member-b-{Guid.NewGuid():N}", "password", "MemberB", CancellationToken.None);
            var group = await groups.CreateAsync("Test Group", CancellationToken.None, memberA.Id, memberB.Id);

            // MessageService.SendMessageToGroupAsync loads the Group by id and enumerates
            // group.GroupUsers in memory — this only works end to end if the GroupUsers navigation
            // is populated by the read, which is exactly what GroupConfiguration's AutoInclude proves.
            var sent = await messages.SendMessageToGroupAsync(sender.Id, group.Id, "hello group", CancellationToken.None);

            Assert.Equal(2, sent.Count);
            Assert.Contains(sent, message => message.ReceiverId == memberA.Id);
            Assert.Contains(sent, message => message.ReceiverId == memberB.Id);
        }

        [Fact]
        public async Task GetUserContacts_ReadsSenderThroughTheAutoIncludedNavigation()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"contact-sender-{Guid.NewGuid():N}", "password", "ContactSender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"contact-receiver-{Guid.NewGuid():N}", "password", "ContactReceiver", CancellationToken.None);
            await messages.SendMessageAsync(sender.Id, receiver.Id, "hi", CancellationToken.None);

            // UserService.GetUserContactsAsync projects Message.Sender in memory after loading by
            // ReceiverId — this throws a NullReferenceException instead of returning contacts if
            // MessageConfiguration's AutoInclude on Sender isn't wired up.
            var contacts = await users.GetUserContactsAsync(receiver.Id, CancellationToken.None);

            Assert.Contains(contacts, contact => contact.Id == sender.Id);
        }

        [Fact]
        public async Task AddUserToGroup_IsReflectedInGetUserGroups()
        {
            var (users, groups, _, _) = CreateServices(fixture);
            var member = await users.RegisterAsync($"joiner-{Guid.NewGuid():N}", "password", "Joiner", CancellationToken.None);
            var group = await groups.CreateAsync("Membership Group", CancellationToken.None);

            await groups.AddUserToGroupAsync(group.Id, member.Id, CancellationToken.None);
            var memberGroups = await users.GetUserGroupsAsync(member.Id, CancellationToken.None);

            Assert.Contains(memberGroups, g => g.Id == group.Id);
        }

        [Fact]
        public async Task AddingTheSameUserToAGroupTwice_ViolatesTheUniqueIndex()
        {
            var (users, groups, _, _) = CreateServices(fixture);
            var member = await users.RegisterAsync($"twice-{Guid.NewGuid():N}", "password", "Twice", CancellationToken.None);
            var group = await groups.CreateAsync("Duplicate Membership Group", CancellationToken.None);
            await groups.AddUserToGroupAsync(group.Id, member.Id, CancellationToken.None);

            // Group.AddUser has no in-memory duplicate check of its own (see GroupUserConfiguration's
            // comment) — the unique index is the only thing that actually prevents this.
            // AddUserToGroupAsync saves immediately, so the second call itself is what throws.
            await Assert.ThrowsAsync<DbUpdateException>(() => groups.AddUserToGroupAsync(group.Id, member.Id, CancellationToken.None));
        }

        [Fact]
        public void AddChatChannel_RegistersEntityFrameworkCoreChatContextThroughDI()
        {
            var services = new ServiceCollection();

            services.AddChatChannel(options => options.UseSqlite("Data Source=:memory:"));

            var serviceProvider = services.BuildServiceProvider();
            var chatContext = serviceProvider.GetRequiredService<EntityFrameworkCoreChatContext>();

            Assert.NotNull(chatContext);
        }
    }
}
