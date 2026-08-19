using Microsoft.AspNetCore.Identity;
using ThunderPropagator.Channels.Chat.InMemory;
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
        private static (UserService Users, GroupService Groups, MessageService Messages, InMemoryChatStore Store) CreateServices()
        {
            var store = new InMemoryChatStore();
            var context = new InMemoryChatContext(store);
            var passwordHasher = new PasswordHasher<User>();

            return (new UserService(context, passwordHasher), new GroupService(context), new MessageService(context), store);
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

        [Fact]
        public async Task GetUserContacts_ReadsSenderThroughThePopulatedNavigation()
        {
            var (users, _, messages, _) = CreateServices();
            var sender = await users.RegisterAsync("contact-sender", "password", "ContactSender", CancellationToken.None);
            var receiver = await users.RegisterAsync("contact-receiver", "password", "ContactReceiver", CancellationToken.None);
            await messages.SendMessageAsync(sender.Id, receiver.Id, "hi", CancellationToken.None);

            var contacts = await users.GetUserContactsAsync(receiver.Id, CancellationToken.None);

            Assert.Contains(contacts, contact => contact.Id == sender.Id);
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
    }
}
