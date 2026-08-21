namespace ThunderPropagator.Channels.Chat.Models.Groups
{
    internal
#if !DEBUG
        sealed
#endif
        class GroupService(IChatContext chatContext)
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
            var group = await GetByIdAsync(groupId, cancellationToken) ?? throw new GroupNotFoundException();
            if (group.IsDeleted)
                throw new GroupNotFoundException();

            if (group.GroupUsers.All(groupUser => groupUser.UserId != currentUserId))
                throw new GroupAccessDeniedException();

            return group;
        }

        public Task<Group> CreateAsync(string name, Guid createdByUserId, CancellationToken cancellationToken = default, params Guid[] users)
        {
            var group = Group.Create(name, createdByUserId);

            foreach (var user in users)
                group.AddUser(user);

            return chatContext.CreateAsync(group, cancellationToken);
        }

        // Issue #124: excludes soft-deleted groups — GetAllAsync<Group>(CancellationToken) has no
        // predicate to filter with, so a deleted group would otherwise still show up here even
        // though it's gone from every other group-facing operation below.
        public async Task<IReadOnlyCollection<Group>> GetAllAsync(CancellationToken cancellationToken = default)
            => await chatContext.GetAllAsync<Group>(group => !group.IsDeleted, cancellationToken);

        public async Task AddUserToGroupAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            var group = await GetByIdAsync(groupId, cancellationToken) ?? throw new GroupNotFoundException();
            if (group.IsDeleted)
                throw new GroupNotFoundException();

            group.AddUser(userId);
            await chatContext.UpdateAsync(group, cancellationToken);
        }

        public async Task RemoveUserFromGroupAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            var group = await GetByIdAsync(groupId, cancellationToken) ?? throw new GroupNotFoundException();
            if (group.IsDeleted)
                throw new GroupNotFoundException();

            group.RemoveUser(userId);
            await chatContext.UpdateAsync(group, cancellationToken);
        }

        public async Task<Group> RenameGroupAsync(Guid groupId, string name, CancellationToken cancellationToken = default)
        {
            var group = await GetByIdAsync(groupId, cancellationToken) ?? throw new GroupNotFoundException();
            if (group.IsDeleted)
                throw new GroupNotFoundException();

            group.SetName(name);
            await chatContext.UpdateAsync(group, cancellationToken);
            return group;
        }

        public async Task<Group> SetGroupIconAsync(Guid groupId, string icon, CancellationToken cancellationToken = default)
        {
            var group = await GetByIdAsync(groupId, cancellationToken) ?? throw new GroupNotFoundException();
            if (group.IsDeleted)
                throw new GroupNotFoundException();

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
