using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Chat.EntityFrameworkCore;
using ThunderPropagator.Channels.Chat.Models;
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

        // Issue #115: GetContactsAsync composes a single server-side query (a Message subquery feeding
        // an IN filter on Users) rather than loading Messages and projecting Sender in memory, so it no
        // longer depends on MessageConfiguration's AutoInclude at all. These five cases are this
        // provider's share of the AC's "empty, duplicate, sent-only, received-only, and bidirectional"
        // contract coverage.
        [Fact]
        public async Task GetContacts_WithNoMessages_ReturnsEmpty()
        {
            var (users, _, _, _) = CreateServices(fixture);
            var user = await users.RegisterAsync($"lonely-{Guid.NewGuid():N}", "password", "Lonely", CancellationToken.None);

            var contacts = await users.GetUserContactsAsync(user.Id, CancellationToken.None);

            Assert.Empty(contacts);
        }

        [Fact]
        public async Task GetContacts_WithDuplicateMessagesFromTheSameContact_ReturnsThatContactOnce()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var user = await users.RegisterAsync($"dup-owner-{Guid.NewGuid():N}", "password", "DupOwner", CancellationToken.None);
            var contact = await users.RegisterAsync($"dup-contact-{Guid.NewGuid():N}", "password", "DupContact", CancellationToken.None);
            await messages.SendMessageAsync(contact.Id, user.Id, "hi", CancellationToken.None);
            await messages.SendMessageAsync(contact.Id, user.Id, "hi again", CancellationToken.None);
            await messages.SendMessageAsync(user.Id, contact.Id, "hey back", CancellationToken.None);

            var contacts = await users.GetUserContactsAsync(user.Id, CancellationToken.None);

            Assert.Single(contacts, c => c.Id == contact.Id);
        }

        [Fact]
        public async Task GetContacts_WithOnlySentMessages_IncludesTheReceiver()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"sent-only-sender-{Guid.NewGuid():N}", "password", "SentOnlySender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"sent-only-receiver-{Guid.NewGuid():N}", "password", "SentOnlyReceiver", CancellationToken.None);
            await messages.SendMessageAsync(sender.Id, receiver.Id, "hi", CancellationToken.None);

            var contacts = await users.GetUserContactsAsync(sender.Id, CancellationToken.None);

            Assert.Contains(contacts, contact => contact.Id == receiver.Id);
        }

        [Fact]
        public async Task GetContacts_WithOnlyReceivedMessages_IncludesTheSender()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"received-only-sender-{Guid.NewGuid():N}", "password", "ReceivedOnlySender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"received-only-receiver-{Guid.NewGuid():N}", "password", "ReceivedOnlyReceiver", CancellationToken.None);
            await messages.SendMessageAsync(sender.Id, receiver.Id, "hi", CancellationToken.None);

            var contacts = await users.GetUserContactsAsync(receiver.Id, CancellationToken.None);

            Assert.Contains(contacts, contact => contact.Id == sender.Id);
        }

        [Fact]
        public async Task GetContacts_WithBidirectionalHistory_IncludesEveryDistinctCounterparty()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var user = await users.RegisterAsync($"bidi-user-{Guid.NewGuid():N}", "password", "BidiUser", CancellationToken.None);
            var sentTo = await users.RegisterAsync($"bidi-sent-to-{Guid.NewGuid():N}", "password", "BidiSentTo", CancellationToken.None);
            var receivedFrom = await users.RegisterAsync($"bidi-received-from-{Guid.NewGuid():N}", "password", "BidiReceivedFrom", CancellationToken.None);
            await messages.SendMessageAsync(user.Id, sentTo.Id, "outgoing", CancellationToken.None);
            await messages.SendMessageAsync(receivedFrom.Id, user.Id, "incoming", CancellationToken.None);

            var contacts = await users.GetUserContactsAsync(user.Id, CancellationToken.None);

            Assert.Equal(2, contacts.Count);
            Assert.Contains(contacts, contact => contact.Id == sentTo.Id);
            Assert.Contains(contacts, contact => contact.Id == receivedFrom.Id);
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

        // Issue #116: SendMessageToGroupAsync used to call UpdateAsync(group) after fanning the
        // message out to every member, even though nothing about the group itself had changed — an
        // unnecessary write that could trip concurrency checks or change-tracking side effects. The
        // spy wraps the real context rather than asserting on ChangeTracker/database internals, so it
        // would catch any future write to Group, not just this specific regression.
        [Fact]
        public async Task SendMessageToGroup_PerformsNoGroupUpdate()
        {
            var (users, groups, _, dbContext) = CreateServices(fixture);
            var spy = new UpdateCountingChatContext(new EntityFrameworkCoreChatContext(dbContext));
            var messages = new MessageService(spy);
            var sender = await users.RegisterAsync($"group-update-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var member = await users.RegisterAsync($"group-update-member-{Guid.NewGuid():N}", "password", "Member", CancellationToken.None);
            var group = await groups.CreateAsync("Update Spy Group", CancellationToken.None, member.Id);

            await messages.SendMessageToGroupAsync(sender.Id, group.Id, "hello group", CancellationToken.None);

            Assert.DoesNotContain(typeof(Group), spy.UpdatedTypes);
        }

        // Issue #117: contract coverage for MessageService.GetDirectMessageHistoryAsync/
        // GetGroupMessageHistoryAsync against a real SQLite database — boundaries, ordering, total
        // count, empty pages, and authorization, mirroring the AC's coverage list and this file's own
        // GetContacts_* coverage for #115.
        [Fact]
        public async Task GetDirectMessageHistory_ReturnsPagesNewestFirstWithoutDuplicatesOrGaps()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var alice = await users.RegisterAsync($"history-alice-{Guid.NewGuid():N}", "password", "Alice", CancellationToken.None);
            var bob = await users.RegisterAsync($"history-bob-{Guid.NewGuid():N}", "password", "Bob", CancellationToken.None);
            for (var i = 0; i < 5; i++)
                await messages.SendMessageAsync(alice.Id, bob.Id, $"message {i}", CancellationToken.None);

            var firstPage = await messages.GetDirectMessageHistoryAsync(alice.Id, bob.Id, page: 1, pageSize: 2, CancellationToken.None);
            var secondPage = await messages.GetDirectMessageHistoryAsync(alice.Id, bob.Id, page: 2, pageSize: 2, CancellationToken.None);
            var thirdPage = await messages.GetDirectMessageHistoryAsync(alice.Id, bob.Id, page: 3, pageSize: 2, CancellationToken.None);

            Assert.Equal(5, firstPage.TotalCount);
            Assert.Equal(2, firstPage.Messages.Count);
            Assert.Equal(2, secondPage.Messages.Count);
            Assert.Single(thirdPage.Messages);
            var allMessages = firstPage.Messages.Concat(secondPage.Messages).Concat(thirdPage.Messages).ToList();
            Assert.Equal(5, allMessages.Select(m => m.Id).Distinct().Count());
            Assert.Equal(["message 4", "message 3", "message 2", "message 1", "message 0"], allMessages.Select(m => m.Body));
        }

        [Fact]
        public async Task GetDirectMessageHistory_PageBeyondRange_ReturnsEmptyWithCorrectTotalCount()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var alice = await users.RegisterAsync($"beyond-alice-{Guid.NewGuid():N}", "password", "Alice", CancellationToken.None);
            var bob = await users.RegisterAsync($"beyond-bob-{Guid.NewGuid():N}", "password", "Bob", CancellationToken.None);
            await messages.SendMessageAsync(alice.Id, bob.Id, "only message", CancellationToken.None);

            var page = await messages.GetDirectMessageHistoryAsync(alice.Id, bob.Id, page: 2, pageSize: 10, CancellationToken.None);

            Assert.Empty(page.Messages);
            Assert.Equal(1, page.TotalCount);
        }

        [Fact]
        public async Task GetDirectMessageHistory_ExcludesGroupFannedOutMessagesBetweenTheSamePair()
        {
            var (users, groups, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"exclude-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var member = await users.RegisterAsync($"exclude-member-{Guid.NewGuid():N}", "password", "Member", CancellationToken.None);
            var group = await groups.CreateAsync("Exclude Group", CancellationToken.None, member.Id);
            await messages.SendMessageToGroupAsync(sender.Id, group.Id, "group message", CancellationToken.None);
            await messages.SendMessageAsync(sender.Id, member.Id, "direct message", CancellationToken.None);

            var page = await messages.GetDirectMessageHistoryAsync(sender.Id, member.Id, page: 1, pageSize: 10, CancellationToken.None);

            Assert.Single(page.Messages);
            Assert.Equal("direct message", page.Messages.Single().Body);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(1, 0)]
        [InlineData(1, MessageService.MaxPageSize + 1)]
        public async Task GetDirectMessageHistory_WithOutOfBoundsPaging_Throws(int page, int pageSize)
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var alice = await users.RegisterAsync($"bounds-alice-{Guid.NewGuid():N}", "password", "Alice", CancellationToken.None);
            var bob = await users.RegisterAsync($"bounds-bob-{Guid.NewGuid():N}", "password", "Bob", CancellationToken.None);

            await Assert.ThrowsAsync<InvalidMessageHistoryPageRequestException>(
                () => messages.GetDirectMessageHistoryAsync(alice.Id, bob.Id, page, pageSize, CancellationToken.None));
        }

        [Fact]
        public async Task GetGroupMessageHistory_ByAMember_ReturnsPagesNewestFirst()
        {
            var (users, groups, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"group-history-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var member = await users.RegisterAsync($"group-history-member-{Guid.NewGuid():N}", "password", "Member", CancellationToken.None);
            var group = await groups.CreateAsync("History Group", CancellationToken.None, member.Id);
            await messages.SendMessageToGroupAsync(sender.Id, group.Id, "first", CancellationToken.None);
            await messages.SendMessageToGroupAsync(sender.Id, group.Id, "second", CancellationToken.None);

            var page = await messages.GetGroupMessageHistoryAsync(member.Id, group.Id, page: 1, pageSize: 10, CancellationToken.None);

            Assert.Equal(2, page.TotalCount);
            Assert.Equal(2, page.Messages.Count);
        }

        [Fact]
        public async Task GetGroupMessageHistory_ByANonMember_Throws()
        {
            var (users, groups, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"nonmember-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var member = await users.RegisterAsync($"nonmember-member-{Guid.NewGuid():N}", "password", "Member", CancellationToken.None);
            var outsider = await users.RegisterAsync($"nonmember-outsider-{Guid.NewGuid():N}", "password", "Outsider", CancellationToken.None);
            var group = await groups.CreateAsync("Members Only Group", CancellationToken.None, member.Id);
            await messages.SendMessageToGroupAsync(sender.Id, group.Id, "secret", CancellationToken.None);

            await Assert.ThrowsAsync<GroupAccessDeniedException>(
                () => messages.GetGroupMessageHistoryAsync(outsider.Id, group.Id, page: 1, pageSize: 10, CancellationToken.None));
        }

        [Fact]
        public async Task GetGroupMessageHistory_ForAMissingGroup_ThrowsGroupNotFound()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var user = await users.RegisterAsync($"missing-group-user-{Guid.NewGuid():N}", "password", "User", CancellationToken.None);

            await Assert.ThrowsAsync<GroupNotFoundException>(
                () => messages.GetGroupMessageHistoryAsync(user.Id, Guid.NewGuid(), page: 1, pageSize: 10, CancellationToken.None));
        }
    }

    // Issue #116: forwards every IChatContext call to the wrapped context while recording which
    // entity types UpdateAsync was called with, so a test can assert an operation performed no write
    // to a given entity type without depending on any one provider's internal storage.
    internal sealed class UpdateCountingChatContext(IChatContext inner) : IChatContext
    {
        private readonly List<Type> _updatedTypes = [];

        public IReadOnlyList<Type> UpdatedTypes => _updatedTypes;

        public Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
            => inner.GetAsync(expression, cancellationToken);

        public Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
            => inner.GetAsync<TEntity, TPk>(id, cancellationToken);

        public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
            => inner.GetAllAsync(expression, cancellationToken);

        public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
            => inner.GetAllAsync<TEntity>(cancellationToken);

        public Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
            => inner.CreateAsync(entity, cancellationToken);

        public Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            _updatedTypes.Add(typeof(TEntity));
            return inner.UpdateAsync(entity, cancellationToken);
        }

        public Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
            => inner.DeleteAsync<TEntity, TPk>(id, cancellationToken);

        public Task<IReadOnlyCollection<User>> GetContactsAsync(Guid userId, CancellationToken cancellationToken = default)
            => inner.GetContactsAsync(userId, cancellationToken);

        public Task<MessageHistoryPage> GetDirectMessageHistoryAsync(Guid userId, Guid otherUserId, int page, int pageSize, CancellationToken cancellationToken = default)
            => inner.GetDirectMessageHistoryAsync(userId, otherUserId, page, pageSize, cancellationToken);

        public Task<MessageHistoryPage> GetGroupMessageHistoryAsync(Guid groupId, int page, int pageSize, CancellationToken cancellationToken = default)
            => inner.GetGroupMessageHistoryAsync(groupId, page, pageSize, cancellationToken);
    }
}
