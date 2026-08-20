using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Chat;
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
        private static (UserService Users, GroupService Groups, MessageService Messages, ChatDbContext DbContext) CreateServices(ChatDatabaseFixture fixture, ChatChannelConfiguration? configuration = null)
        {
            var dbContext = fixture.CreateDbContext();
            var chatContext = new EntityFrameworkCoreChatContext(dbContext);
            var passwordHasher = new PasswordHasher<User>();

            return (new UserService(chatContext, passwordHasher), new GroupService(chatContext), new MessageService(chatContext, configuration ?? new ChatChannelConfiguration()), dbContext);
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
            var messages = new MessageService(spy, new ChatChannelConfiguration());
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
