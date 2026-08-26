using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Chat;
using ThunderPropagator.Channels.Chat.Configuration;
using ThunderPropagator.Channels.Chat.EntityFrameworkCore;
using ThunderPropagator.Channels.Chat.EntityFrameworkCore.Context;
using ThunderPropagator.Channels.Chat.EntityFrameworkCore.Extensions;
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
        private static (UserService Users, GroupService Groups, MessageService Messages, ChatDbContext DbContext) CreateServices(ChatDatabaseFixture fixture, ChatChannelConfiguration? configuration = null)
        {
            var dbContext = fixture.CreateDbContext();
            var chatContext = new EntityFrameworkCoreChatContext(dbContext);
            var passwordHasher = new PasswordHasher<User>();
            var resolvedConfiguration = configuration ?? new ChatChannelConfiguration();

            return (new UserService(chatContext, passwordHasher, resolvedConfiguration), new GroupService(chatContext, resolvedConfiguration), new MessageService(chatContext, resolvedConfiguration), dbContext);
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
            var group = await groups.CreateAsync("Test Group", sender.Id, [memberA.Id, memberB.Id], CancellationToken.None);

            // MessageService.SendMessageToGroupAsync loads the Group by id and enumerates
            // group.GroupUsers in memory — this only works end to end if the GroupUsers navigation
            // is populated by the read, which is exactly what GroupConfiguration's AutoInclude proves.
            var sent = await messages.SendMessageToGroupAsync(sender.Id, group.Id, "hello group", CancellationToken.None);

            Assert.Equal(2, sent.Count);
            Assert.Contains(sent, message => message.ReceiverId == memberA.Id);
            Assert.Contains(sent, message => message.ReceiverId == memberB.Id);
        }

        // Issue #142: the navigation-loading contract every IChatContext provider follows — Message.
        // Sender is always populated after a read (MessageConfiguration's AutoInclude), Receiver/
        // Group never are — proven here for EntityFrameworkCore specifically, mirroring the
        // equivalent InMemory/MongoDB coverage. A fresh DbContext forces a real query rather than
        // returning the already-tracked instance CreateAsync handed back, which never touched Sender
        // at all.
        [Fact]
        public async Task Message_AfterARead_HasSenderPopulatedButNotReceiverOrGroup()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"nav-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"nav-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "hello", CancellationToken.None);

            var freshContext = new EntityFrameworkCoreChatContext(fixture.CreateDbContext());
            var fetched = await freshContext.GetAsync<Message, Guid>(sent.Id, CancellationToken.None);

            Assert.NotNull(fetched!.Sender);
            Assert.Equal(sender.Id, fetched.Sender.Id);
            Assert.Null(fetched.Receiver);
            Assert.Null(fetched.Group);
        }

        // Issue #142: the other half of the contract — Group.GroupUsers is always populated after a
        // read, and GroupUser.User is never populated (not loaded by this query at all, so there's
        // nothing for EF to fix up). GroupUser.Group is deliberately NOT asserted null here — see its
        // own doc comment: EntityFrameworkCore's change-tracking incidentally fixes it up whenever a
        // Group and its GroupUsers are loaded together in the same tracked context, which is exactly
        // what happens via GroupConfiguration's AutoInclude. That's provider-specific incidental
        // behavior, not a contract guarantee either way, so no test should assert a particular value
        // for it — InMemory/MongoDB's equivalent coverage does assert null, since neither provider
        // has this tracking mechanism at all.
        [Fact]
        public async Task GroupUser_AfterARead_DoesNotHaveItsUserPopulated()
        {
            var (users, groups, _, _) = CreateServices(fixture);
            var creator = await users.RegisterAsync($"nav-creator-{Guid.NewGuid():N}", "password", "Creator", CancellationToken.None);
            var member = await users.RegisterAsync($"nav-member-{Guid.NewGuid():N}", "password", "Member", CancellationToken.None);
            var group = await groups.CreateAsync("Nav Group", creator.Id, [member.Id], CancellationToken.None);

            var freshContext = new EntityFrameworkCoreChatContext(fixture.CreateDbContext());
            var fetched = await freshContext.GetAsync<Group, Guid>(group.Id, CancellationToken.None);

            var groupUser = Assert.Single(fetched!.GroupUsers);
            Assert.Null(groupUser.User);
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
            var group = await groups.CreateAsync("Membership Group", member.Id, [], CancellationToken.None);

            await groups.AddUserToGroupAsync(group.Id, member.Id, CancellationToken.None);
            var memberGroups = await users.GetUserGroupsAsync(member.Id, CancellationToken.None);

            Assert.Contains(memberGroups, g => g.Id == group.Id);
        }

        [Fact]
        public async Task AddingTheSameUserToAGroupTwice_ViolatesTheUniqueIndex()
        {
            var (users, groups, _, _) = CreateServices(fixture);
            var member = await users.RegisterAsync($"twice-{Guid.NewGuid():N}", "password", "Twice", CancellationToken.None);
            var group = await groups.CreateAsync("Duplicate Membership Group", member.Id, [], CancellationToken.None);
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
            var messages = new MessageService(spy, new ChatChannelConfiguration());
            var sender = await users.RegisterAsync($"group-update-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var member = await users.RegisterAsync($"group-update-member-{Guid.NewGuid():N}", "password", "Member", CancellationToken.None);
            var group = await groups.CreateAsync("Update Spy Group", sender.Id, [member.Id], CancellationToken.None);

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
            var group = await groups.CreateAsync("Exclude Group", sender.Id, [member.Id], CancellationToken.None);
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
            var group = await groups.CreateAsync("History Group", sender.Id, [member.Id], CancellationToken.None);
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
            var group = await groups.CreateAsync("Members Only Group", sender.Id, [member.Id], CancellationToken.None);
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

        // Issue #119: contract coverage for MessageService.DeleteMessageAsync against a real SQLite
        // database — success, forbidden, missing, repeated, and concurrent deletion, mirroring the
        // AC's coverage list and this file's own GetGroupMessageHistory_* coverage for #117.
        [Fact]
        public async Task DeleteMessage_BySender_MarksItDeletedAndRedactsBodyAndExcludesItFromHistory()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"delete-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"delete-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "secret", CancellationToken.None);

            var deleted = await messages.DeleteMessageAsync(sender.Id, sent.Id, CancellationToken.None);

            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
            Assert.Equal(string.Empty, deleted.Body);
            var page = await messages.GetDirectMessageHistoryAsync(sender.Id, receiver.Id, page: 1, pageSize: 10, CancellationToken.None);
            Assert.Empty(page.Messages);
        }

        [Fact]
        public async Task DeleteMessage_ByANonSender_ThrowsAndLeavesTheMessageUnaffected()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"forbidden-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"forbidden-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var outsider = await users.RegisterAsync($"forbidden-outsider-{Guid.NewGuid():N}", "password", "Outsider", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "secret", CancellationToken.None);

            await Assert.ThrowsAsync<MessageDeleteForbiddenException>(
                () => messages.DeleteMessageAsync(outsider.Id, sent.Id, CancellationToken.None));

            var page = await messages.GetDirectMessageHistoryAsync(sender.Id, receiver.Id, page: 1, pageSize: 10, CancellationToken.None);
            Assert.Single(page.Messages, m => m.Id == sent.Id && m.Body == "secret");
        }

        [Fact]
        public async Task DeleteMessage_ForAMissingMessage_ThrowsMessageNotFound()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var user = await users.RegisterAsync($"missing-message-user-{Guid.NewGuid():N}", "password", "User", CancellationToken.None);

            await Assert.ThrowsAsync<MessageNotFoundException>(
                () => messages.DeleteMessageAsync(user.Id, Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task DeleteMessage_CalledTwiceBySender_IsIdempotent()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"repeat-delete-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"repeat-delete-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "secret", CancellationToken.None);
            var firstDelete = await messages.DeleteMessageAsync(sender.Id, sent.Id, CancellationToken.None);

            var secondDelete = await messages.DeleteMessageAsync(sender.Id, sent.Id, CancellationToken.None);

            Assert.True(secondDelete.IsDeleted);
            Assert.Equal(firstDelete.DeletedAt, secondDelete.DeletedAt);
        }

        [Fact]
        public async Task DeleteMessage_CalledConcurrentlyBySender_DoesNotThrowAndEndsUpDeleted()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"concurrent-delete-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"concurrent-delete-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "secret", CancellationToken.None);

            // Two independent MessageService instances, each against its own DbContext (as two real
            // concurrent requests would be, one per scope) — dbContext alone isn't safe to share
            // across concurrent EF Core operations.
            var chatContext = new EntityFrameworkCoreChatContext(fixture.CreateDbContext());
            var otherMessages = new MessageService(chatContext, new ChatChannelConfiguration());

            await Task.WhenAll(
                messages.DeleteMessageAsync(sender.Id, sent.Id, CancellationToken.None),
                otherMessages.DeleteMessageAsync(sender.Id, sent.Id, CancellationToken.None));

            var page = await messages.GetDirectMessageHistoryAsync(sender.Id, receiver.Id, page: 1, pageSize: 10, CancellationToken.None);
            Assert.Empty(page.Messages);
        }

        // Issue #124: contract coverage for GroupService.DeleteGroupAsync against a real SQLite
        // database — creator success, non-creator forbidden, missing group, repeated (idempotent)
        // deletion, and concurrent deletion, plus the admin-can-delete-group-message extension to
        // MessageService.DeleteMessageAsync, mirroring the InMemory project's own coverage.
        [Fact]
        public async Task DeleteGroup_ByItsCreator_SoftDeletesItClearsMembersAndReturnsThemAsAffected()
        {
            var (users, groups, _, _) = CreateServices(fixture);
            var creator = await users.RegisterAsync($"delete-group-creator-{Guid.NewGuid():N}", "password", "Creator", CancellationToken.None);
            var memberA = await users.RegisterAsync($"delete-group-member-a-{Guid.NewGuid():N}", "password", "MemberA", CancellationToken.None);
            var memberB = await users.RegisterAsync($"delete-group-member-b-{Guid.NewGuid():N}", "password", "MemberB", CancellationToken.None);
            var group = await groups.CreateAsync("Doomed Group", creator.Id, [memberA.Id, memberB.Id], CancellationToken.None);

            var (deleted, affectedMemberIds) = await groups.DeleteGroupAsync(creator.Id, group.Id, CancellationToken.None);

            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
            Assert.Empty(deleted.GroupUsers);
            Assert.Equal(new[] { memberA.Id, memberB.Id }.OrderBy(id => id), affectedMemberIds.OrderBy(id => id));
            var allGroups = await groups.GetAllAsync(CancellationToken.None);
            Assert.DoesNotContain(allGroups, g => g.Id == group.Id);
        }

        [Fact]
        public async Task DeleteGroup_ByANonCreatorMember_ThrowsAndLeavesTheGroupUnaffected()
        {
            var (users, groups, _, _) = CreateServices(fixture);
            var creator = await users.RegisterAsync($"forbidden-delete-creator-{Guid.NewGuid():N}", "password", "Creator", CancellationToken.None);
            var member = await users.RegisterAsync($"forbidden-delete-member-{Guid.NewGuid():N}", "password", "Member", CancellationToken.None);
            var group = await groups.CreateAsync("Protected Group", creator.Id, [member.Id], CancellationToken.None);

            await Assert.ThrowsAsync<GroupDeleteForbiddenException>(
                () => groups.DeleteGroupAsync(member.Id, group.Id, CancellationToken.None));

            var stillThere = await groups.GetByIdAsync(group.Id, CancellationToken.None);
            Assert.False(stillThere!.IsDeleted);
        }

        [Fact]
        public async Task DeleteGroup_ForAMissingGroup_ThrowsGroupNotFound()
        {
            var (users, groups, _, _) = CreateServices(fixture);
            var user = await users.RegisterAsync($"missing-group-delete-user-{Guid.NewGuid():N}", "password", "User", CancellationToken.None);

            await Assert.ThrowsAsync<GroupNotFoundException>(
                () => groups.DeleteGroupAsync(user.Id, Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task DeleteGroup_CalledTwiceByCreator_IsIdempotent()
        {
            var (users, groups, _, _) = CreateServices(fixture);
            var creator = await users.RegisterAsync($"repeat-delete-group-creator-{Guid.NewGuid():N}", "password", "Creator", CancellationToken.None);
            var member = await users.RegisterAsync($"repeat-delete-group-member-{Guid.NewGuid():N}", "password", "Member", CancellationToken.None);
            var group = await groups.CreateAsync("Repeat Delete Group", creator.Id, [member.Id], CancellationToken.None);
            var (firstDeleted, firstAffected) = await groups.DeleteGroupAsync(creator.Id, group.Id, CancellationToken.None);

            var (secondDeleted, secondAffected) = await groups.DeleteGroupAsync(creator.Id, group.Id, CancellationToken.None);

            Assert.True(secondDeleted.IsDeleted);
            Assert.Equal(firstDeleted.DeletedAt, secondDeleted.DeletedAt);
            Assert.Equal([member.Id], firstAffected);
            Assert.Empty(secondAffected);
        }

        [Fact]
        public async Task DeleteGroup_CalledConcurrentlyByCreator_DoesNotThrowAndEndsUpDeleted()
        {
            var (users, groups, _, _) = CreateServices(fixture);
            var creator = await users.RegisterAsync($"concurrent-delete-group-creator-{Guid.NewGuid():N}", "password", "Creator", CancellationToken.None);
            var member = await users.RegisterAsync($"concurrent-delete-group-member-{Guid.NewGuid():N}", "password", "Member", CancellationToken.None);
            var group = await groups.CreateAsync("Concurrent Delete Group", creator.Id, [member.Id], CancellationToken.None);

            // Two independent GroupService instances, each against its own DbContext — see
            // DeleteMessage_CalledConcurrentlyBySender_DoesNotThrowAndEndsUpDeleted's own reasoning.
            var otherGroups = new GroupService(new EntityFrameworkCoreChatContext(fixture.CreateDbContext()), new ChatChannelConfiguration());

            await Task.WhenAll(
                groups.DeleteGroupAsync(creator.Id, group.Id, CancellationToken.None),
                otherGroups.DeleteGroupAsync(creator.Id, group.Id, CancellationToken.None));

            var stillThere = await groups.GetByIdAsync(group.Id, CancellationToken.None);
            Assert.True(stillThere!.IsDeleted);
        }

        [Fact]
        public async Task DeleteMessage_ByTheGroupsCreator_SucceedsEvenThoughTheyDidNotSendIt()
        {
            var (users, groups, messages, _) = CreateServices(fixture);
            var creator = await users.RegisterAsync($"admin-delete-creator-{Guid.NewGuid():N}", "password", "Creator", CancellationToken.None);
            var member = await users.RegisterAsync($"admin-delete-member-{Guid.NewGuid():N}", "password", "Member", CancellationToken.None);
            var group = await groups.CreateAsync("Moderated Group", creator.Id, [member.Id], CancellationToken.None);
            var sent = await messages.SendMessageToGroupAsync(member.Id, group.Id, "off-topic", CancellationToken.None);

            var deleted = await messages.DeleteMessageAsync(creator.Id, sent.Single().Id, CancellationToken.None);

            Assert.True(deleted.IsDeleted);
        }

        [Fact]
        public async Task DeleteMessage_ByTheCreatorOfADifferentGroup_ThrowsForbidden()
        {
            var (users, groups, messages, _) = CreateServices(fixture);
            var creator = await users.RegisterAsync($"wrong-group-creator-{Guid.NewGuid():N}", "password", "Creator", CancellationToken.None);
            var otherCreator = await users.RegisterAsync($"other-group-creator-{Guid.NewGuid():N}", "password", "OtherCreator", CancellationToken.None);
            var member = await users.RegisterAsync($"wrong-group-member-{Guid.NewGuid():N}", "password", "Member", CancellationToken.None);
            var group = await groups.CreateAsync("Actual Group", creator.Id, [member.Id], CancellationToken.None);
            await groups.CreateAsync("Unrelated Group", otherCreator.Id, [], CancellationToken.None);
            var sent = await messages.SendMessageToGroupAsync(member.Id, group.Id, "hello", CancellationToken.None);

            await Assert.ThrowsAsync<MessageDeleteForbiddenException>(
                () => messages.DeleteMessageAsync(otherCreator.Id, sent.Single().Id, CancellationToken.None));
        }

        [Fact]
        public async Task SendMessageToGroup_AfterTheGroupWasDeleted_ThrowsGroupNotFound()
        {
            var (users, groups, messages, _) = CreateServices(fixture);
            var creator = await users.RegisterAsync($"send-to-deleted-creator-{Guid.NewGuid():N}", "password", "Creator", CancellationToken.None);
            var member = await users.RegisterAsync($"send-to-deleted-member-{Guid.NewGuid():N}", "password", "Member", CancellationToken.None);
            var group = await groups.CreateAsync("Soon Deleted Group", creator.Id, [member.Id], CancellationToken.None);
            await groups.DeleteGroupAsync(creator.Id, group.Id, CancellationToken.None);

            await Assert.ThrowsAsync<GroupNotFoundException>(
                () => messages.SendMessageToGroupAsync(creator.Id, group.Id, "too late", CancellationToken.None));
        }

        // Issue #120: contract coverage for MessageService.EditMessageAsync against a real SQLite
        // database — success, forbidden, missing, time-window boundaries, invalid content, and
        // concurrent edits, mirroring the AC's coverage list and this file's own DeleteMessage_*
        // coverage for #119.
        [Fact]
        public async Task EditMessage_BySenderWithinWindow_UpdatesBodyAndMarksItEditedAndHistoryReflectsIt()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"edit-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"edit-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "original", CancellationToken.None);

            var edited = await messages.EditMessageAsync(sender.Id, sent.Id, "revised", CancellationToken.None);

            Assert.True(edited.IsEdited);
            Assert.NotNull(edited.EditedAt);
            Assert.Equal("revised", edited.Body);
            var page = await messages.GetDirectMessageHistoryAsync(sender.Id, receiver.Id, page: 1, pageSize: 10, CancellationToken.None);
            Assert.Single(page.Messages, m => m.Id == sent.Id && m.Body == "revised");
        }

        [Fact]
        public async Task EditMessage_ByANonSender_ThrowsAndLeavesTheMessageUnaffected()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"edit-forbidden-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"edit-forbidden-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var outsider = await users.RegisterAsync($"edit-forbidden-outsider-{Guid.NewGuid():N}", "password", "Outsider", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "original", CancellationToken.None);

            await Assert.ThrowsAsync<MessageEditForbiddenException>(
                () => messages.EditMessageAsync(outsider.Id, sent.Id, "revised", CancellationToken.None));

            var page = await messages.GetDirectMessageHistoryAsync(sender.Id, receiver.Id, page: 1, pageSize: 10, CancellationToken.None);
            Assert.Single(page.Messages, m => m.Id == sent.Id && m.Body == "original");
        }

        [Fact]
        public async Task EditMessage_ForAMissingMessage_ThrowsMessageNotFound()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var user = await users.RegisterAsync($"edit-missing-message-user-{Guid.NewGuid():N}", "password", "User", CancellationToken.None);

            await Assert.ThrowsAsync<MessageNotFoundException>(
                () => messages.EditMessageAsync(user.Id, Guid.NewGuid(), "revised", CancellationToken.None));
        }

        [Fact]
        public async Task EditMessage_ForAnAlreadyDeletedMessage_ThrowsMessageNotFound()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"edit-deleted-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"edit-deleted-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "original", CancellationToken.None);
            await messages.DeleteMessageAsync(sender.Id, sent.Id, CancellationToken.None);

            await Assert.ThrowsAsync<MessageNotFoundException>(
                () => messages.EditMessageAsync(sender.Id, sent.Id, "revised", CancellationToken.None));
        }

        [Fact]
        public async Task EditMessage_WithABlankBody_ThrowsInvalidMessageEdit()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"edit-blank-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"edit-blank-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "original", CancellationToken.None);

            await Assert.ThrowsAsync<InvalidMessageEditException>(
                () => messages.EditMessageAsync(sender.Id, sent.Id, "   ", CancellationToken.None));
        }

        [Fact]
        public async Task EditMessage_AfterTheConfiguredWindowHasElapsed_ThrowsWindowExpired()
        {
            // A zero-length window means the real (tiny but nonzero) elapsed time between
            // SendMessageAsync and EditMessageAsync already exceeds it — deterministic, no artificial
            // delay needed to exercise the "expired" boundary.
            var (users, _, messages, _) = CreateServices(fixture, new ChatChannelConfiguration { MessageEditWindow = TimeSpan.Zero });
            var sender = await users.RegisterAsync($"edit-expired-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"edit-expired-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "original", CancellationToken.None);

            await Assert.ThrowsAsync<MessageEditWindowExpiredException>(
                () => messages.EditMessageAsync(sender.Id, sent.Id, "revised", CancellationToken.None));
        }

        [Fact]
        public async Task EditMessage_WithinAGenerousWindow_Succeeds()
        {
            var (users, _, messages, _) = CreateServices(fixture, new ChatChannelConfiguration { MessageEditWindow = TimeSpan.FromMinutes(15) });
            var sender = await users.RegisterAsync($"edit-window-ok-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"edit-window-ok-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "original", CancellationToken.None);

            var edited = await messages.EditMessageAsync(sender.Id, sent.Id, "revised", CancellationToken.None);

            Assert.Equal("revised", edited.Body);
        }

        [Fact]
        public async Task EditMessage_CalledConcurrentlyBySender_DoesNotThrowAndOneRevisionWins()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"edit-concurrent-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"edit-concurrent-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "original", CancellationToken.None);

            // Two independent MessageService instances, each against its own DbContext, mirroring
            // DeleteMessage_CalledConcurrentlyBySender_DoesNotThrowAndEndsUpDeleted's own reasoning.
            var chatContext = new EntityFrameworkCoreChatContext(fixture.CreateDbContext());
            var otherMessages = new MessageService(chatContext, new ChatChannelConfiguration());

            await Task.WhenAll(
                messages.EditMessageAsync(sender.Id, sent.Id, "revision A", CancellationToken.None),
                otherMessages.EditMessageAsync(sender.Id, sent.Id, "revision B", CancellationToken.None));

            var page = await messages.GetDirectMessageHistoryAsync(sender.Id, receiver.Id, page: 1, pageSize: 10, CancellationToken.None);
            var stored = Assert.Single(page.Messages, m => m.Id == sent.Id);
            Assert.True(stored.IsEdited);
            Assert.True(stored.Body is "revision A" or "revision B", $"Expected either revision, got '{stored.Body}'.");
        }

        // Issue #125: contract coverage for MessageService.MarkMessagesReadAsync against a real
        // SQLite database — recipient success, non-recipient/missing/deleted failures folded into the
        // same partial-failure bucket, batch requests with a mix of valid and invalid ids, idempotent
        // repeats, and concurrent calls, mirroring the InMemory project's own coverage.
        [Fact]
        public async Task MarkMessagesRead_ByTheRecipient_MarksItReadAndReturnsItAsSucceeded()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"mark-read-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"mark-read-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "hello", CancellationToken.None);

            var result = await messages.MarkMessagesReadAsync(receiver.Id, [sent.Id], CancellationToken.None);

            var marked = Assert.Single(result.MarkedRead);
            Assert.Equal(sent.Id, marked.Id);
            Assert.True(marked.IsRead);
            Assert.NotNull(marked.ReadAt);
            Assert.Empty(result.FailedMessageIds);
        }

        [Fact]
        public async Task MarkMessagesRead_ByANonRecipient_FailsWithoutThrowingAndLeavesTheMessageUnread()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"mark-read-forbidden-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"mark-read-forbidden-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var outsider = await users.RegisterAsync($"mark-read-forbidden-outsider-{Guid.NewGuid():N}", "password", "Outsider", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "hello", CancellationToken.None);

            var result = await messages.MarkMessagesReadAsync(outsider.Id, [sent.Id], CancellationToken.None);

            Assert.Empty(result.MarkedRead);
            Assert.Equal([sent.Id], result.FailedMessageIds);
            var page = await messages.GetDirectMessageHistoryAsync(sender.Id, receiver.Id, page: 1, pageSize: 10, CancellationToken.None);
            Assert.False(page.Messages.Single().IsRead);
        }

        [Fact]
        public async Task MarkMessagesRead_ForAMissingMessageId_FailsWithoutThrowing()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var user = await users.RegisterAsync($"mark-read-missing-user-{Guid.NewGuid():N}", "password", "User", CancellationToken.None);

            var result = await messages.MarkMessagesReadAsync(user.Id, [Guid.NewGuid()], CancellationToken.None);

            Assert.Empty(result.MarkedRead);
            Assert.Single(result.FailedMessageIds);
        }

        [Fact]
        public async Task MarkMessagesRead_ForAnAlreadyDeletedMessage_FailsWithoutThrowing()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"mark-read-deleted-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"mark-read-deleted-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "hello", CancellationToken.None);
            await messages.DeleteMessageAsync(sender.Id, sent.Id, CancellationToken.None);

            var result = await messages.MarkMessagesReadAsync(receiver.Id, [sent.Id], CancellationToken.None);

            Assert.Empty(result.MarkedRead);
            Assert.Equal([sent.Id], result.FailedMessageIds);
        }

        [Fact]
        public async Task MarkMessagesRead_WithAMixOfValidAndInvalidIds_ReturnsBothBucketsCorrectly()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"mark-read-batch-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"mark-read-batch-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var outsider = await users.RegisterAsync($"mark-read-batch-outsider-{Guid.NewGuid():N}", "password", "Outsider", CancellationToken.None);
            var ownMessage = await messages.SendMessageAsync(sender.Id, receiver.Id, "for receiver", CancellationToken.None);
            var othersMessage = await messages.SendMessageAsync(sender.Id, outsider.Id, "for outsider", CancellationToken.None);
            var missingId = Guid.NewGuid();

            var result = await messages.MarkMessagesReadAsync(receiver.Id, [ownMessage.Id, othersMessage.Id, missingId], CancellationToken.None);

            Assert.Equal([ownMessage.Id], result.MarkedRead.Select(m => m.Id));
            Assert.Equal(
                new[] { othersMessage.Id, missingId }.OrderBy(id => id),
                result.FailedMessageIds.OrderBy(id => id));
        }

        [Fact]
        public async Task MarkMessagesRead_CalledTwiceByRecipient_IsIdempotent()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"mark-read-repeat-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"mark-read-repeat-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "hello", CancellationToken.None);
            var firstResult = await messages.MarkMessagesReadAsync(receiver.Id, [sent.Id], CancellationToken.None);

            var secondResult = await messages.MarkMessagesReadAsync(receiver.Id, [sent.Id], CancellationToken.None);

            Assert.Equal(firstResult.MarkedRead.Single().ReadAt, secondResult.MarkedRead.Single().ReadAt);
        }

        [Fact]
        public async Task MarkMessagesRead_CalledConcurrentlyByRecipient_DoesNotThrowAndEndsUpRead()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var sender = await users.RegisterAsync($"mark-read-concurrent-sender-{Guid.NewGuid():N}", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync($"mark-read-concurrent-receiver-{Guid.NewGuid():N}", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "hello", CancellationToken.None);

            // Two independent MessageService instances, each against its own DbContext, mirroring
            // DeleteMessage_CalledConcurrentlyBySender_DoesNotThrowAndEndsUpDeleted's own reasoning.
            var otherMessages = new MessageService(new EntityFrameworkCoreChatContext(fixture.CreateDbContext()), new ChatChannelConfiguration());

            await Task.WhenAll(
                messages.MarkMessagesReadAsync(receiver.Id, [sent.Id], CancellationToken.None),
                otherMessages.MarkMessagesReadAsync(receiver.Id, [sent.Id], CancellationToken.None));

            var page = await messages.GetDirectMessageHistoryAsync(sender.Id, receiver.Id, page: 1, pageSize: 10, CancellationToken.None);
            Assert.True(page.Messages.Single().IsRead);
        }

        // Issue #123: contract coverage for UserService.SearchUsersAsync against a real SQLite
        // database — username/name matching, normalization, paging, term validation, and
        // cancellation, mirroring the AC's coverage list.
        [Fact]
        public async Task SearchUsers_MatchesByUsernameSubstring_CaseInsensitively()
        {
            var (users, _, _, _) = CreateServices(fixture);
            var suffix = Guid.NewGuid().ToString("N");
            await users.RegisterAsync($"alice-wonder-{suffix}", "password", "Someone Else", CancellationToken.None);
            await users.RegisterAsync($"bob-{suffix}", "password", "Bob", CancellationToken.None);

            var page = await users.SearchUsersAsync($"WONDER-{suffix}".ToUpperInvariant(), page: 1, pageSize: 10, CancellationToken.None);

            Assert.Single(page.Users, u => u.UserName == $"alice-wonder-{suffix}");
        }

        [Fact]
        public async Task SearchUsers_MatchesByNameSubstring_CaseInsensitively()
        {
            var (users, _, _, _) = CreateServices(fixture);
            var suffix = Guid.NewGuid().ToString("N");
            var username = $"carol-{suffix}";
            await users.RegisterAsync(username, "password", $"Carol Danvers {suffix}", CancellationToken.None);
            await users.RegisterAsync($"dave-{suffix}", "password", "Dave", CancellationToken.None);

            var page = await users.SearchUsersAsync($"danvers {suffix}", page: 1, pageSize: 10, CancellationToken.None);

            Assert.Single(page.Users, u => u.UserName == username);
        }

        [Fact]
        public async Task SearchUsers_TrimsSurroundingWhitespaceFromTheTerm()
        {
            var (users, _, _, _) = CreateServices(fixture);
            var username = $"erin-{Guid.NewGuid():N}";
            await users.RegisterAsync(username, "password", "Erin", CancellationToken.None);

            var page = await users.SearchUsersAsync($"  {username}  ", page: 1, pageSize: 10, CancellationToken.None);

            Assert.Single(page.Users, u => u.UserName == username);
        }

        [Fact]
        public async Task SearchUsers_ReturnsPagesWithoutDuplicatesOrGaps()
        {
            var (users, _, _, _) = CreateServices(fixture);
            var suffix = Guid.NewGuid().ToString("N");
            var term = $"search-target-{suffix}";
            for (var i = 0; i < 5; i++)
                await users.RegisterAsync($"{term}-{i}", "password", $"Target {i}", CancellationToken.None);

            var firstPage = await users.SearchUsersAsync(term, page: 1, pageSize: 2, CancellationToken.None);
            var secondPage = await users.SearchUsersAsync(term, page: 2, pageSize: 2, CancellationToken.None);
            var thirdPage = await users.SearchUsersAsync(term, page: 3, pageSize: 2, CancellationToken.None);

            Assert.Equal(5, firstPage.TotalCount);
            Assert.Equal(2, firstPage.Users.Count);
            Assert.Equal(2, secondPage.Users.Count);
            Assert.Single(thirdPage.Users);
            var allIds = firstPage.Users.Concat(secondPage.Users).Concat(thirdPage.Users).Select(u => u.Id).ToList();
            Assert.Equal(5, allIds.Distinct().Count());
        }

        [Fact]
        public async Task SearchUsers_WithAnEmptyTerm_ThrowsInvalidUserSearchRequest()
        {
            var (users, _, _, _) = CreateServices(fixture);

            await Assert.ThrowsAsync<InvalidUserSearchRequestException>(
                () => users.SearchUsersAsync(string.Empty, page: 1, pageSize: 10, CancellationToken.None));
        }

        [Fact]
        public async Task SearchUsers_WithATooShortTerm_ThrowsInvalidUserSearchRequest()
        {
            var (users, _, _, _) = CreateServices(fixture);

            await Assert.ThrowsAsync<InvalidUserSearchRequestException>(
                () => users.SearchUsersAsync("a", page: 1, pageSize: 10, CancellationToken.None));
        }

        [Fact]
        public async Task SearchUsers_WithAnOversizedTerm_ThrowsInvalidUserSearchRequest()
        {
            var (users, _, _, _) = CreateServices(fixture);
            var oversizedTerm = new string('a', UserService.MaxSearchTermLength + 1);

            await Assert.ThrowsAsync<InvalidUserSearchRequestException>(
                () => users.SearchUsersAsync(oversizedTerm, page: 1, pageSize: 10, CancellationToken.None));
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(1, 0)]
        [InlineData(1, UserService.MaxPageSize + 1)]
        public async Task SearchUsers_WithOutOfBoundsPaging_ThrowsInvalidUserSearchRequest(int page, int pageSize)
        {
            var (users, _, _, _) = CreateServices(fixture);

            await Assert.ThrowsAsync<InvalidUserSearchRequestException>(
                () => users.SearchUsersAsync("valid-term", page, pageSize, CancellationToken.None));
        }

        [Fact]
        public async Task SearchUsers_WithAnAlreadyCancelledToken_Throws()
        {
            var chatContext = new EntityFrameworkCoreChatContext(fixture.CreateDbContext());
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => chatContext.SearchUsersAsync("term", 1, 10, cts.Token));
        }

        // Issue #126: contract coverage for UserService.GetOnlineContactsAsync against a real SQLite
        // database — privacy, duplicate connection ids, pagination, and paging validation, mirroring
        // the InMemory project's own coverage.
        [Fact]
        public async Task GetOnlineContacts_OnlyReturnsContactsWhoAreOnline_NeverAnOnlineStranger()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var currentUser = await users.RegisterAsync($"online-current-{Guid.NewGuid():N}", "password", "Current", CancellationToken.None);
            var onlineContact = await users.RegisterAsync($"online-contact-{Guid.NewGuid():N}", "password", "OnlineContact", CancellationToken.None);
            var offlineContact = await users.RegisterAsync($"offline-contact-{Guid.NewGuid():N}", "password", "OfflineContact", CancellationToken.None);
            var onlineStranger = await users.RegisterAsync($"online-stranger-{Guid.NewGuid():N}", "password", "OnlineStranger", CancellationToken.None);
            await messages.SendMessageAsync(currentUser.Id, onlineContact.Id, "hi", CancellationToken.None);
            await messages.SendMessageAsync(currentUser.Id, offlineContact.Id, "hi", CancellationToken.None);

            var page = await users.GetOnlineContactsAsync(currentUser.Id, [onlineContact.Id, onlineStranger.Id], page: 1, pageSize: 10, CancellationToken.None);

            Assert.Equal(1, page.TotalCount);
            Assert.Equal(onlineContact.Id, page.Users.Single().Id);
        }

        [Fact]
        public async Task GetOnlineContacts_WithDuplicateConnectionIdsForTheSameUser_ReturnsThatUserOnce()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var currentUser = await users.RegisterAsync($"dedupe-current-{Guid.NewGuid():N}", "password", "Current", CancellationToken.None);
            var contact = await users.RegisterAsync($"dedupe-contact-{Guid.NewGuid():N}", "password", "Contact", CancellationToken.None);
            await messages.SendMessageAsync(currentUser.Id, contact.Id, "hi", CancellationToken.None);

            var page = await users.GetOnlineContactsAsync(currentUser.Id, [contact.Id, contact.Id], page: 1, pageSize: 10, CancellationToken.None);

            Assert.Equal(1, page.TotalCount);
            Assert.Equal(contact.Id, page.Users.Single().Id);
        }

        [Fact]
        public async Task GetOnlineContacts_PaginatesResults()
        {
            var (users, _, messages, _) = CreateServices(fixture);
            var currentUser = await users.RegisterAsync($"online-page-current-{Guid.NewGuid():N}", "password", "Current", CancellationToken.None);
            var contactIds = new List<Guid>();
            for (var i = 0; i < 5; i++)
            {
                var contact = await users.RegisterAsync($"online-page-contact-{i}-{Guid.NewGuid():N}", "password", $"Contact{i}", CancellationToken.None);
                await messages.SendMessageAsync(currentUser.Id, contact.Id, "hi", CancellationToken.None);
                contactIds.Add(contact.Id);
            }

            var firstPage = await users.GetOnlineContactsAsync(currentUser.Id, contactIds, page: 1, pageSize: 2, CancellationToken.None);
            var secondPage = await users.GetOnlineContactsAsync(currentUser.Id, contactIds, page: 2, pageSize: 2, CancellationToken.None);
            var thirdPage = await users.GetOnlineContactsAsync(currentUser.Id, contactIds, page: 3, pageSize: 2, CancellationToken.None);

            Assert.Equal(5, firstPage.TotalCount);
            Assert.Equal(2, firstPage.Users.Count);
            Assert.Equal(2, secondPage.Users.Count);
            Assert.Single(thirdPage.Users);
            var allIds = firstPage.Users.Concat(secondPage.Users).Concat(thirdPage.Users).Select(u => u.Id).ToList();
            Assert.Equal(5, allIds.Distinct().Count());
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(1, 0)]
        [InlineData(1, UserService.MaxPageSize + 1)]
        public async Task GetOnlineContacts_WithOutOfBoundsPaging_ThrowsInvalidOnlineUsersRequest(int page, int pageSize)
        {
            var (users, _, _, _) = CreateServices(fixture);
            var currentUser = await users.RegisterAsync($"online-bounds-current-{Guid.NewGuid():N}", "password", "Current", CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOnlineUsersRequestException>(
                () => users.GetOnlineContactsAsync(currentUser.Id, [], page, pageSize, CancellationToken.None));
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

        public Task<UserSearchPage> SearchUsersAsync(string normalizedTerm, int page, int pageSize, CancellationToken cancellationToken = default)
            => inner.SearchUsersAsync(normalizedTerm, page, pageSize, cancellationToken);
    }
}
