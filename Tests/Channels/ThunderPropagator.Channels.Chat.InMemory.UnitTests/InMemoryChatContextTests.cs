using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using ThunderPropagator.Channels.Chat;
using ThunderPropagator.Channels.Chat.InMemory;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.InMemory
{
    /// <summary>
    /// Issue #112: exercises InMemoryChatContext through UserService/GroupService/MessageService —
    /// the same real application code the Chat channel's own pipelines use — rather than
    /// reimplementing register/login/group-membership logic in the tests, the same approach #110/#111
    /// use for their integration tests. Unlike those two, nothing here needs a live server or
    /// container: an in-memory store has no external dependency to fake or skip.
    /// </summary>
    public sealed class InMemoryChatContextTests
    {
        private static (UserService Users, GroupService Groups, MessageService Messages, InMemoryChatStore Store) CreateServices(ChatChannelConfiguration? configuration = null)
        {
            var store = new InMemoryChatStore();
            var context = new InMemoryChatContext(store);
            var passwordHasher = new PasswordHasher<User>();

            return (new UserService(context, passwordHasher), new GroupService(context), new MessageService(context, configuration ?? new ChatChannelConfiguration()), store);
        }

        [Fact]
        public async Task RegisterThenLogin_RoundTripsAUser()
        {
            var (users, _, _, _) = CreateServices();

            await users.RegisterAsync("alice", "correct horse battery staple", "Alice", CancellationToken.None);
            var loggedIn = await users.LoginAsync("alice", "correct horse battery staple", CancellationToken.None);

            Assert.Equal("alice", loggedIn.UserName);
            Assert.Equal("Alice", loggedIn.Name);
        }

        [Fact]
        public async Task RegisteringTheSameUsernameTwice_Throws()
        {
            var (users, _, _, _) = CreateServices();
            await users.RegisterAsync("dup", "password-one", "First", CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => users.RegisterAsync("dup", "password-two", "Second", CancellationToken.None));
        }

        [Fact]
        public async Task RegisteringTheSameUsernameTwice_BypassingTheServiceCheck_ViolatesTheStoresUniqueConstraint()
        {
            // UserService.RegisterAsync already checks for an existing username itself — this
            // bypasses that in-app check to prove the store-level constraint (InMemoryChatStore's own
            // EnsureUnique) is what actually stops a duplicate from ever being persisted, not just the
            // service's own pre-check. Mirrors #110/#111's equivalent test.
            var (_, _, _, store) = CreateServices();
            var context = new InMemoryChatContext(store);
            var first = User.Create("dup2", "First");
            first.SetPasswordHash("hash");
            var second = User.Create("dup2", "Second");
            second.SetPasswordHash("hash");
            await context.CreateAsync(first, CancellationToken.None);

            await Assert.ThrowsAsync<InMemoryUniqueConstraintException>(() => context.CreateAsync(second, CancellationToken.None));
        }

        [Fact]
        public async Task MutatingTheEntityPassedToCreateAsync_AfterTheCallReturns_DoesNotAffectTheStore()
        {
            // The core design guarantee InMemoryEntityCloner exists for: an in-memory provider that
            // stored the exact reference CreateAsync received would let a caller silently "persist" a
            // change just by mutating the object it already had, without ever calling UpdateAsync —
            // exactly the ad-hoc-in-memory-implementation bug #112 exists to rule out. A read-only
            // clone on the way out isn't enough to catch this: it only proves GetAsync doesn't hand
            // out a mutable reference, not that CreateAsync/UpdateAsync stored an independent copy in
            // the first place. Mutating the original directly (not through a second fetch-then-update
            // cycle, which a read-side clone alone would already isolate) is what actually exercises
            // the write-side clone.
            var context = new InMemoryChatContext(new InMemoryChatStore());
            var user = User.Create("mutate-on-write", "Original Name");
            user.SetPasswordHash("hash");
            await context.CreateAsync(user, CancellationToken.None);

            user.SetName("Mutated After Create");
            var stored = await context.GetAsync<User, Guid>(user.Id, CancellationToken.None);

            Assert.Equal("Original Name", stored!.Name);
        }

        [Fact]
        public async Task GroupUsers_IsPopulatedAfterCreate_SoSendMessageToGroupReachesEveryMember()
        {
            var (users, groups, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("sender", "password", "Sender", CancellationToken.None);
            var memberA = await users.RegisterAsync("member-a", "password", "MemberA", CancellationToken.None);
            var memberB = await users.RegisterAsync("member-b", "password", "MemberB", CancellationToken.None);
            var group = await groups.CreateAsync("Test Group", CancellationToken.None, memberA.Id, memberB.Id);

            var sent = await messages.SendMessageToGroupAsync(sender.Id, group.Id, "hello group", CancellationToken.None);

            Assert.Equal(2, sent.Count);
            Assert.Contains(sent, message => message.ReceiverId == memberA.Id);
            Assert.Contains(sent, message => message.ReceiverId == memberB.Id);
        }

        // Issue #115: GetContactsAsync reads SenderId/ReceiverId straight off the stored Message
        // entries rather than the populated Sender navigation. These five cases are this provider's
        // share of the AC's "empty, duplicate, sent-only, received-only, and bidirectional" contract
        // coverage.
        [Fact]
        public async Task GetContacts_WithNoMessages_ReturnsEmpty()
        {
            var (users, _, _, _) = CreateServices();
            var user = await users.RegisterAsync("lonely", "password", "Lonely", CancellationToken.None);

            var contacts = await users.GetUserContactsAsync(user.Id, CancellationToken.None);

            Assert.Empty(contacts);
        }

        [Fact]
        public async Task GetContacts_WithDuplicateMessagesFromTheSameContact_ReturnsThatContactOnce()
        {
            var (users, _, messages, _) = CreateServices();
            var user = await users.RegisterAsync("dup-owner", "password", "DupOwner", CancellationToken.None);
            var contact = await users.RegisterAsync("dup-contact", "password", "DupContact", CancellationToken.None);
            await messages.SendMessageAsync(contact.Id, user.Id, "hi", CancellationToken.None);
            await messages.SendMessageAsync(contact.Id, user.Id, "hi again", CancellationToken.None);
            await messages.SendMessageAsync(user.Id, contact.Id, "hey back", CancellationToken.None);

            var contacts = await users.GetUserContactsAsync(user.Id, CancellationToken.None);

            Assert.Single(contacts, c => c.Id == contact.Id);
        }

        [Fact]
        public async Task GetContacts_WithOnlySentMessages_IncludesTheReceiver()
        {
            var (users, _, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("sent-only-sender", "password", "SentOnlySender", CancellationToken.None);
            var receiver = await users.RegisterAsync("sent-only-receiver", "password", "SentOnlyReceiver", CancellationToken.None);
            await messages.SendMessageAsync(sender.Id, receiver.Id, "hi", CancellationToken.None);

            var contacts = await users.GetUserContactsAsync(sender.Id, CancellationToken.None);

            Assert.Contains(contacts, contact => contact.Id == receiver.Id);
        }

        [Fact]
        public async Task GetContacts_WithOnlyReceivedMessages_IncludesTheSender()
        {
            var (users, _, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("received-only-sender", "password", "ReceivedOnlySender", CancellationToken.None);
            var receiver = await users.RegisterAsync("received-only-receiver", "password", "ReceivedOnlyReceiver", CancellationToken.None);
            await messages.SendMessageAsync(sender.Id, receiver.Id, "hi", CancellationToken.None);

            var contacts = await users.GetUserContactsAsync(receiver.Id, CancellationToken.None);

            Assert.Contains(contacts, contact => contact.Id == sender.Id);
        }

        [Fact]
        public async Task GetContacts_WithBidirectionalHistory_IncludesEveryDistinctCounterparty()
        {
            var (users, _, messages, _) = CreateServices();
            var user = await users.RegisterAsync("bidi-user", "password", "BidiUser", CancellationToken.None);
            var sentTo = await users.RegisterAsync("bidi-sent-to", "password", "BidiSentTo", CancellationToken.None);
            var receivedFrom = await users.RegisterAsync("bidi-received-from", "password", "BidiReceivedFrom", CancellationToken.None);
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
            var (users, groups, _, _) = CreateServices();
            var member = await users.RegisterAsync("joiner", "password", "Joiner", CancellationToken.None);
            var group = await groups.CreateAsync("Membership Group", CancellationToken.None);

            await groups.AddUserToGroupAsync(group.Id, member.Id, CancellationToken.None);
            var memberGroups = await users.GetUserGroupsAsync(member.Id, CancellationToken.None);

            Assert.Contains(memberGroups, g => g.Id == group.Id);
        }

        [Fact]
        public async Task AddingTheSameUserToAGroupTwice_Throws()
        {
            var (users, groups, _, _) = CreateServices();
            var member = await users.RegisterAsync("twice", "password", "Twice", CancellationToken.None);
            var group = await groups.CreateAsync("Duplicate Membership Group", CancellationToken.None);
            await groups.AddUserToGroupAsync(group.Id, member.Id, CancellationToken.None);

            // Group.AddUser has no in-memory duplicate check of its own (GroupUser doesn't override
            // Equals/GetHashCode) — the store's unique constraint is the only thing that prevents this.
            await Assert.ThrowsAsync<InMemoryUniqueConstraintException>(
                () => groups.AddUserToGroupAsync(group.Id, member.Id, CancellationToken.None));
        }

        [Fact]
        public async Task DeletingAGroup_CascadesItsGroupUsers()
        {
            var store = new InMemoryChatStore();
            var context = new InMemoryChatContext(store);
            var group = Group.Create("Cascade Group").AddUser(Guid.NewGuid());
            await context.CreateAsync(group, CancellationToken.None);

            var deleted = await context.DeleteAsync<Group, Guid>(group.Id, CancellationToken.None);
            var remainingGroupUsers = await context.GetAllAsync<GroupUser>(CancellationToken.None);

            Assert.True(deleted);
            Assert.Empty(remainingGroupUsers);
        }

        [Fact]
        public async Task GetAsync_WithAnAlreadyCancelledToken_Throws()
        {
            var store = new InMemoryChatStore();
            var context = new InMemoryChatContext(store);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => context.GetAllAsync<User>(cts.Token));
        }

        // Issue #116: SendMessageToGroupAsync used to call UpdateAsync(group) after fanning the
        // message out to every member, even though nothing about the group itself had changed — an
        // unnecessary write that could trip concurrency checks or change-tracking side effects. The
        // spy wraps the real context rather than asserting on store internals, so it would catch any
        // future write to Group, not just this specific regression.
        [Fact]
        public async Task SendMessageToGroup_PerformsNoGroupUpdate()
        {
            var store = new InMemoryChatStore();
            var users = new UserService(new InMemoryChatContext(store), new PasswordHasher<User>());
            var groups = new GroupService(new InMemoryChatContext(store));
            var spy = new UpdateCountingChatContext(new InMemoryChatContext(store));
            var messages = new MessageService(spy, new ChatChannelConfiguration());
            var sender = await users.RegisterAsync("group-update-sender", "password", "Sender", CancellationToken.None);
            var member = await users.RegisterAsync("group-update-member", "password", "Member", CancellationToken.None);
            var group = await groups.CreateAsync("Update Spy Group", CancellationToken.None, member.Id);

            await messages.SendMessageToGroupAsync(sender.Id, group.Id, "hello group", CancellationToken.None);

            Assert.DoesNotContain(typeof(Group), spy.UpdatedTypes);
        }

        // Issue #117: contract coverage for MessageService.GetDirectMessageHistoryAsync/
        // GetGroupMessageHistoryAsync — boundaries, ordering, total count, empty pages, and
        // authorization, mirroring the AC's coverage list.
        [Fact]
        public async Task GetDirectMessageHistory_ReturnsPagesNewestFirstWithoutDuplicatesOrGaps()
        {
            var (users, _, messages, _) = CreateServices();
            var alice = await users.RegisterAsync("history-alice", "password", "Alice", CancellationToken.None);
            var bob = await users.RegisterAsync("history-bob", "password", "Bob", CancellationToken.None);
            for (var i = 0; i < 5; i++)
                await messages.SendMessageAsync(alice.Id, bob.Id, $"message {i}", CancellationToken.None);

            var firstPage = await messages.GetDirectMessageHistoryAsync(alice.Id, bob.Id, page: 1, pageSize: 2, CancellationToken.None);
            var secondPage = await messages.GetDirectMessageHistoryAsync(alice.Id, bob.Id, page: 2, pageSize: 2, CancellationToken.None);
            var thirdPage = await messages.GetDirectMessageHistoryAsync(alice.Id, bob.Id, page: 3, pageSize: 2, CancellationToken.None);

            Assert.Equal(5, firstPage.TotalCount);
            Assert.Equal(2, firstPage.Messages.Count);
            Assert.Equal(2, secondPage.Messages.Count);
            Assert.Single(thirdPage.Messages);
            var seenIds = firstPage.Messages.Concat(secondPage.Messages).Concat(thirdPage.Messages).Select(m => m.Id).ToList();
            Assert.Equal(5, seenIds.Distinct().Count());
            var bodiesNewestFirst = seenIds
                .Select(id => firstPage.Messages.Concat(secondPage.Messages).Concat(thirdPage.Messages).Single(m => m.Id == id).Body)
                .ToList();
            Assert.Equal(["message 4", "message 3", "message 2", "message 1", "message 0"], bodiesNewestFirst);
        }

        [Fact]
        public async Task GetDirectMessageHistory_PageBeyondRange_ReturnsEmptyWithCorrectTotalCount()
        {
            var (users, _, messages, _) = CreateServices();
            var alice = await users.RegisterAsync("beyond-alice", "password", "Alice", CancellationToken.None);
            var bob = await users.RegisterAsync("beyond-bob", "password", "Bob", CancellationToken.None);
            await messages.SendMessageAsync(alice.Id, bob.Id, "only message", CancellationToken.None);

            var page = await messages.GetDirectMessageHistoryAsync(alice.Id, bob.Id, page: 2, pageSize: 10, CancellationToken.None);

            Assert.Empty(page.Messages);
            Assert.Equal(1, page.TotalCount);
        }

        [Fact]
        public async Task GetDirectMessageHistory_ExcludesGroupFannedOutMessagesBetweenTheSamePair()
        {
            var (users, groups, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("exclude-sender", "password", "Sender", CancellationToken.None);
            var member = await users.RegisterAsync("exclude-member", "password", "Member", CancellationToken.None);
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
            var (users, _, messages, _) = CreateServices();
            var alice = await users.RegisterAsync("bounds-alice", "password", "Alice", CancellationToken.None);
            var bob = await users.RegisterAsync("bounds-bob", "password", "Bob", CancellationToken.None);

            await Assert.ThrowsAsync<InvalidMessageHistoryPageRequestException>(
                () => messages.GetDirectMessageHistoryAsync(alice.Id, bob.Id, page, pageSize, CancellationToken.None));
        }

        [Fact]
        public async Task GetGroupMessageHistory_ByAMember_ReturnsPagesNewestFirst()
        {
            var (users, groups, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("group-history-sender", "password", "Sender", CancellationToken.None);
            var member = await users.RegisterAsync("group-history-member", "password", "Member", CancellationToken.None);
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
            var (users, groups, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("nonmember-sender", "password", "Sender", CancellationToken.None);
            var member = await users.RegisterAsync("nonmember-member", "password", "Member", CancellationToken.None);
            var outsider = await users.RegisterAsync("nonmember-outsider", "password", "Outsider", CancellationToken.None);
            var group = await groups.CreateAsync("Members Only Group", CancellationToken.None, member.Id);
            await messages.SendMessageToGroupAsync(sender.Id, group.Id, "secret", CancellationToken.None);

            await Assert.ThrowsAsync<GroupAccessDeniedException>(
                () => messages.GetGroupMessageHistoryAsync(outsider.Id, group.Id, page: 1, pageSize: 10, CancellationToken.None));
        }

        [Fact]
        public async Task GetGroupMessageHistory_ForAMissingGroup_ThrowsGroupNotFound()
        {
            var (users, _, messages, _) = CreateServices();
            var user = await users.RegisterAsync("missing-group-user", "password", "User", CancellationToken.None);

            await Assert.ThrowsAsync<GroupNotFoundException>(
                () => messages.GetGroupMessageHistoryAsync(user.Id, Guid.NewGuid(), page: 1, pageSize: 10, CancellationToken.None));
        }

        [Fact]
        public async Task GetDirectMessageHistory_WithAnAlreadyCancelledToken_Throws()
        {
            var store = new InMemoryChatStore();
            var context = new InMemoryChatContext(store);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => context.GetDirectMessageHistoryAsync(Guid.NewGuid(), Guid.NewGuid(), 1, 10, cts.Token));
        }

        // Issue #119: contract coverage for MessageService.DeleteMessageAsync — success, forbidden,
        // missing, repeated, and concurrent deletion, mirroring the AC's coverage list.
        [Fact]
        public async Task DeleteMessage_BySender_MarksItDeletedAndRedactsBodyAndExcludesItFromHistory()
        {
            var (users, _, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("delete-sender", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync("delete-receiver", "password", "Receiver", CancellationToken.None);
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
            var (users, _, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("forbidden-sender", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync("forbidden-receiver", "password", "Receiver", CancellationToken.None);
            var outsider = await users.RegisterAsync("forbidden-outsider", "password", "Outsider", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "secret", CancellationToken.None);

            await Assert.ThrowsAsync<MessageDeleteForbiddenException>(
                () => messages.DeleteMessageAsync(outsider.Id, sent.Id, CancellationToken.None));

            var page = await messages.GetDirectMessageHistoryAsync(sender.Id, receiver.Id, page: 1, pageSize: 10, CancellationToken.None);
            Assert.Single(page.Messages, m => m.Id == sent.Id && m.Body == "secret");
        }

        [Fact]
        public async Task DeleteMessage_ForAMissingMessage_ThrowsMessageNotFound()
        {
            var (users, _, messages, _) = CreateServices();
            var user = await users.RegisterAsync("missing-message-user", "password", "User", CancellationToken.None);

            await Assert.ThrowsAsync<MessageNotFoundException>(
                () => messages.DeleteMessageAsync(user.Id, Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task DeleteMessage_CalledTwiceBySender_IsIdempotent()
        {
            var (users, _, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("repeat-delete-sender", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync("repeat-delete-receiver", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "secret", CancellationToken.None);
            var firstDelete = await messages.DeleteMessageAsync(sender.Id, sent.Id, CancellationToken.None);

            var secondDelete = await messages.DeleteMessageAsync(sender.Id, sent.Id, CancellationToken.None);

            Assert.True(secondDelete.IsDeleted);
            Assert.Equal(firstDelete.DeletedAt, secondDelete.DeletedAt);
        }

        [Fact]
        public async Task DeleteMessage_CalledConcurrentlyBySender_DoesNotThrowAndEndsUpDeleted()
        {
            var (users, _, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("concurrent-delete-sender", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync("concurrent-delete-receiver", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "secret", CancellationToken.None);

            await Task.WhenAll(
                messages.DeleteMessageAsync(sender.Id, sent.Id, CancellationToken.None),
                messages.DeleteMessageAsync(sender.Id, sent.Id, CancellationToken.None));

            var page = await messages.GetDirectMessageHistoryAsync(sender.Id, receiver.Id, page: 1, pageSize: 10, CancellationToken.None);
            Assert.Empty(page.Messages);
        }

        // Issue #120: contract coverage for MessageService.EditMessageAsync — success, forbidden,
        // missing, time-window boundaries, invalid content, and concurrent edits, mirroring the AC's
        // coverage list.
        [Fact]
        public async Task EditMessage_BySenderWithinWindow_UpdatesBodyAndMarksItEditedAndHistoryReflectsIt()
        {
            var (users, _, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("edit-sender", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync("edit-receiver", "password", "Receiver", CancellationToken.None);
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
            var (users, _, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("edit-forbidden-sender", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync("edit-forbidden-receiver", "password", "Receiver", CancellationToken.None);
            var outsider = await users.RegisterAsync("edit-forbidden-outsider", "password", "Outsider", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "original", CancellationToken.None);

            await Assert.ThrowsAsync<MessageEditForbiddenException>(
                () => messages.EditMessageAsync(outsider.Id, sent.Id, "revised", CancellationToken.None));

            var page = await messages.GetDirectMessageHistoryAsync(sender.Id, receiver.Id, page: 1, pageSize: 10, CancellationToken.None);
            Assert.Single(page.Messages, m => m.Id == sent.Id && m.Body == "original");
        }

        [Fact]
        public async Task EditMessage_ForAMissingMessage_ThrowsMessageNotFound()
        {
            var (users, _, messages, _) = CreateServices();
            var user = await users.RegisterAsync("edit-missing-message-user", "password", "User", CancellationToken.None);

            await Assert.ThrowsAsync<MessageNotFoundException>(
                () => messages.EditMessageAsync(user.Id, Guid.NewGuid(), "revised", CancellationToken.None));
        }

        [Fact]
        public async Task EditMessage_ForAnAlreadyDeletedMessage_ThrowsMessageNotFound()
        {
            var (users, _, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("edit-deleted-sender", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync("edit-deleted-receiver", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "original", CancellationToken.None);
            await messages.DeleteMessageAsync(sender.Id, sent.Id, CancellationToken.None);

            await Assert.ThrowsAsync<MessageNotFoundException>(
                () => messages.EditMessageAsync(sender.Id, sent.Id, "revised", CancellationToken.None));
        }

        [Fact]
        public async Task EditMessage_WithABlankBody_ThrowsInvalidMessageEdit()
        {
            var (users, _, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("edit-blank-sender", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync("edit-blank-receiver", "password", "Receiver", CancellationToken.None);
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
            var (users, _, messages, _) = CreateServices(new ChatChannelConfiguration { MessageEditWindow = TimeSpan.Zero });
            var sender = await users.RegisterAsync("edit-expired-sender", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync("edit-expired-receiver", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "original", CancellationToken.None);

            await Assert.ThrowsAsync<MessageEditWindowExpiredException>(
                () => messages.EditMessageAsync(sender.Id, sent.Id, "revised", CancellationToken.None));
        }

        [Fact]
        public async Task EditMessage_WithinAGenerousWindow_Succeeds()
        {
            var (users, _, messages, _) = CreateServices(new ChatChannelConfiguration { MessageEditWindow = TimeSpan.FromMinutes(15) });
            var sender = await users.RegisterAsync("edit-window-ok-sender", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync("edit-window-ok-receiver", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "original", CancellationToken.None);

            var edited = await messages.EditMessageAsync(sender.Id, sent.Id, "revised", CancellationToken.None);

            Assert.Equal("revised", edited.Body);
        }

        [Fact]
        public async Task EditMessage_CalledConcurrentlyBySender_DoesNotThrowAndOneRevisionWins()
        {
            var (users, _, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("edit-concurrent-sender", "password", "Sender", CancellationToken.None);
            var receiver = await users.RegisterAsync("edit-concurrent-receiver", "password", "Receiver", CancellationToken.None);
            var sent = await messages.SendMessageAsync(sender.Id, receiver.Id, "original", CancellationToken.None);

            await Task.WhenAll(
                messages.EditMessageAsync(sender.Id, sent.Id, "revision A", CancellationToken.None),
                messages.EditMessageAsync(sender.Id, sent.Id, "revision B", CancellationToken.None));

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
