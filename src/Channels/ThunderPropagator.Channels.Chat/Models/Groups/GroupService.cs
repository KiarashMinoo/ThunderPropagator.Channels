using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Configuration;

namespace ThunderPropagator.Channels.Chat.Models.Groups
{
    internal
#if !DEBUG
        sealed
#endif
        class GroupService(IChatContext chatContext, ChatChannelConfiguration configuration)
    {
        public Task<Group?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken = default)
            => chatContext.GetAsync<Group, Guid>(groupId, cancellationToken);

        // Issue #132: no WebSocket pipeline retrieves a single group's details today (Groups/GetAll
        // lists every non-deleted group system-wide, unscoped by membership — see the REST
        // GetGroupsAsync endpoint's own comment on why that pipeline isn't reused for a
        // membership-scoped listing either), so this mirrors the exact membership-check shape
        // MessageService.GetGroupMessageHistoryAsync already established for the group case: the
        // group must exist and not be soft-deleted (GroupNotFoundException), and the caller must be a
        // *current* member of its GroupUsers (GroupAccessDeniedException) — the same former-member
        // policy, no separate creator/administrator bypass, since #124 only ever grants the creator
        // delete/moderation authority, not blanket visibility into a group they've left.
        public async Task<Group> GetGroupDetailsAsync(Guid currentUserId, Guid groupId, CancellationToken cancellationToken = default)
        {
            var group = await GetExistingGroupAsync(groupId, cancellationToken);

            if (group.GroupUsers.All(groupUser => groupUser.UserId != currentUserId))
                throw new GroupAccessDeniedException();

            return group;
        }

        // Existence + not-deleted only, no membership check — shared by GetGroupDetailsAsync (which
        // adds membership) and by the self-join/self-leave paths below (see #31's own remarks on
        // AddUserToGroupAsync/RemoveUserFromGroupAsync), which must not require the caller to already
        // be a member of a group they're in the middle of joining.
        private async Task<Group> GetExistingGroupAsync(Guid groupId, CancellationToken cancellationToken)
        {
            var group = await GetByIdAsync(groupId, cancellationToken) ?? throw new GroupNotFoundException();
            if (group.IsDeleted)
                throw new GroupNotFoundException();

            return group;
        }

        // Issue #136: users used to be added exactly as given — a duplicate id created two distinct
        // GroupUser rows for the same membership (GroupUser has no value equality, so the HashSet
        // Group.GroupUsers uses never deduplicated them), and neither group size nor invited-user
        // existence was checked. This dedupes users, validates the resulting size against
        // MaxGroupMembers, and confirms every invited id resolves to an existing user before creating
        // anything — all as one atomic pre-check, so a request that fails validation never partially
        // persists a group. The creator is deliberately NOT folded into membership here: existing
        // coverage (InMemoryChatContextTests/ChatEntityFrameworkCoreIntegrationTests) already
        // establishes group creation as leaving the creator's own membership to whatever the caller's
        // users array does or doesn't include, and this is the WebSocket Groups/Create pipeline's only
        // call site too, so changing that default would have silently changed already-tested behavior
        // for both transports rather than just adding the missing validation this issue asks for.
        //
        // Issue #140: users used to be a trailing `params Guid[]`, which C# only allows after every
        // other parameter — including the optional cancellationToken — so a caller passing member ids
        // had to also supply a token explicitly just to reach them, even though cancellation is the
        // less commonly varied argument of the two. users now comes right after the two required
        // parameters as a plain IReadOnlyCollection<Guid>, with cancellationToken last and still
        // optional — the conventional .NET shape, and the same relative order the REST/WebSocket
        // call sites' own arguments (name, creator, members, token) already read in. GroupService is
        // internal to this assembly (never part of the public package surface), so this is a pure
        // source-compatibility change: both existing call sites were updated in the same change, and
        // no binary-compatibility shim (e.g. an [Obsolete] overload retaining the old order) is
        // needed — external consumers can never have compiled against this signature. A caller
        // migrating a positional `CreateAsync(name, creator, cancellationToken, u1, u2)` call rewrites
        // it as `CreateAsync(name, creator, [u1, u2], cancellationToken)`.
        public async Task<Group> CreateAsync(string name, Guid createdByUserId, IReadOnlyCollection<Guid> users, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidGroupCreateRequestException("Name cannot be empty.");

            // Issue #38: unbounded before this — a group name of arbitrary size was accepted and
            // persisted as-is, the same unbounded-input gap MaxMessageLength (#141) already closed
            // for message Body.
            if (name.Length > configuration.MaxGroupNameLength)
                throw new InvalidGroupCreateRequestException($"Name must not exceed {configuration.MaxGroupNameLength} characters (was {name.Length}).");

            var memberIds = users.Distinct().ToArray();

            if (memberIds.Length > configuration.MaxGroupMembers)
                throw new InvalidGroupCreateRequestException($"A group cannot have more than {configuration.MaxGroupMembers} members.");

            foreach (var userId in memberIds)
            {
                if (await chatContext.GetAsync<User, Guid>(userId, cancellationToken) is null)
                    throw new InvalidGroupCreateRequestException($"User '{userId}' does not exist.");
            }

            var group = Group.Create(name, createdByUserId);

            foreach (var userId in memberIds)
                group.AddUser(userId);

            return await chatContext.CreateAsync(group, cancellationToken);
        }

        // Issue #124: excludes soft-deleted groups — GetAllAsync<Group>(CancellationToken) has no
        // predicate to filter with, so a deleted group would otherwise still show up here even
        // though it's gone from every other group-facing operation below.
        public async Task<IReadOnlyCollection<Group>> GetAllAsync(CancellationToken cancellationToken = default)
            => await chatContext.GetAllAsync<Group>(group => !group.IsDeleted, cancellationToken);

        // Issue #31: used to fetch the group with plain GetByIdAsync (existence + soft-delete only)
        // and mutate it with no check that currentUserId was actually a member — an IDOR letting any
        // authenticated caller add an arbitrary user to a group they don't belong to. Adding someone
        // else now requires currentUserId to already be a member (GetGroupDetailsAsync's own
        // existence + not-deleted + membership check); adding yourself (self-join — the WebSocket
        // Groups/Join pipeline's only use of this method, currentUserId == userId) is exempt from the
        // membership half of that check by construction, since requiring it would make joining a group
        // you're not yet in impossible.
        public async Task<Group> AddUserToGroupAsync(Guid currentUserId, Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            var group = currentUserId == userId
                ? await GetExistingGroupAsync(groupId, cancellationToken)
                : await GetGroupDetailsAsync(currentUserId, groupId, cancellationToken);

            group.AddUser(userId);
            await chatContext.UpdateAsync(group, cancellationToken);
            return group;
        }

        // Issue #31: same IDOR as AddUserToGroupAsync above, and the same self-exception — removing
        // yourself (self-leave — the WebSocket Groups/UserLeave pipeline's only use of this method,
        // currentUserId == userId) doesn't require already being confirmed a member first, since
        // leaving a group you're not a member of is a harmless no-op (see Group.RemoveUser), not a
        // privilege to guard. Removing someone else requires currentUserId to already be a member.
        public async Task<Group> RemoveUserFromGroupAsync(Guid currentUserId, Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            var group = currentUserId == userId
                ? await GetExistingGroupAsync(groupId, cancellationToken)
                : await GetGroupDetailsAsync(currentUserId, groupId, cancellationToken);

            group.RemoveUser(userId);
            await chatContext.UpdateAsync(group, cancellationToken);
            return group;
        }

        // Issue #38: unlike CreateAsync, this used to enforce no bound on name at all — the caller's
        // string reached Group.SetName directly, which only rejects null/whitespace (via a raw
        // ArgumentException, not this domain's usual BadRequest-mapped exception shape). Applying the
        // same two checks CreateAsync already enforces keeps rename and create consistent with each
        // other and gives a caller the same friendly error contract for either.
        public async Task<Group> RenameGroupAsync(Guid currentUserId, Guid groupId, string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidGroupCreateRequestException("Name cannot be empty.");

            if (name.Length > configuration.MaxGroupNameLength)
                throw new InvalidGroupCreateRequestException($"Name must not exceed {configuration.MaxGroupNameLength} characters (was {name.Length}).");

            var group = await GetGroupDetailsAsync(currentUserId, groupId, cancellationToken);

            group.SetName(name);
            await chatContext.UpdateAsync(group, cancellationToken);
            return group;
        }

        // Issue #38: icon was previously unbounded and unvalidated — any length string was accepted
        // and persisted as-is. Unlike Name, an empty icon is a legitimate "clear the icon" request (the
        // domain's GroupIcon is nullable), so this only caps length, it doesn't require non-empty.
        public async Task<Group> SetGroupIconAsync(Guid currentUserId, Guid groupId, string icon, CancellationToken cancellationToken = default)
        {
            if (icon.Length > configuration.MaxGroupIconLength)
                throw new InvalidGroupIconRequestException($"Icon must not exceed {configuration.MaxGroupIconLength} characters (was {icon.Length}).");

            var group = await GetGroupDetailsAsync(currentUserId, groupId, cancellationToken);

            group.SetGroupIcon(icon);
            await chatContext.UpdateAsync(group, cancellationToken);
            return group;
        }

        // Issue #124: only the group's creator (this domain's only admin concept — see Group's own
        // comment) may delete it. An already-deleted group short-circuits before the write, so a
        // repeated delete request by the creator is idempotent — no error, no redundant persistence,
        // and (since GroupUsers is already empty from the first call) no former members left to
        // notify on the repeat. A genuine concurrent race between two such calls is an accepted
        // trade-off, consistent with #119's equivalent message-delete race — this domain has no
        // concurrency-token infrastructure anywhere else (#116). AffectedMemberIds is captured before
        // MarkDeleted clears GroupUsers, since the pipeline needs to know who to notify after the
        // group they were a member of no longer lists any members at all.
        public async Task<(Group Group, IReadOnlyCollection<Guid> AffectedMemberIds)> DeleteGroupAsync(Guid currentUserId, Guid groupId, CancellationToken cancellationToken = default)
        {
            var group = await GetByIdAsync(groupId, cancellationToken) ?? throw new GroupNotFoundException();

            if (group.CreatedByUserId != currentUserId)
                throw new GroupDeleteForbiddenException();

            if (group.IsDeleted)
                return (group, []);

            var affectedMemberIds = group.GroupUsers.Select(groupUser => groupUser.UserId).ToList();

            group.MarkDeleted();
            await chatContext.UpdateAsync(group, cancellationToken);

            return (group, affectedMemberIds);
        }
    }
}
