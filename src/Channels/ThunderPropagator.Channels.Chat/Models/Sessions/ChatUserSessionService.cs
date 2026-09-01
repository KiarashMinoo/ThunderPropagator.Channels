namespace ThunderPropagator.Channels.Chat.Models.Sessions
{
    // Issue #46: the persisted replacement for ChatChannel's old node-local LoggedInUsers
    // dictionary — see ChatUserSession's own doc comment.
    internal
#if !DEBUG
        sealed
#endif
        class ChatUserSessionService(IChatContext chatContext)
    {
        /// <summary>
        /// Records <paramref name="connectionId"/> as logged in as <paramref name="userId"/>. A
        /// stale row already present for this connectionId (e.g. a reused connection id, which
        /// shouldn't normally happen but isn't this method's concern to rule out) is replaced rather
        /// than rejected — unlike a User's UserName, a connectionId carries no business-uniqueness
        /// meaning worth enforcing as a conflict.
        /// </summary>
        public async Task LogInAsync(string connectionId, Guid userId, CancellationToken cancellationToken = default)
        {
            var existing = await chatContext.GetAsync<ChatUserSession>(session => session.ConnectionId == connectionId, cancellationToken);
            if (existing is not null)
                await chatContext.DeleteAsync<ChatUserSession, Guid>(existing.Id, cancellationToken);

            await chatContext.CreateAsync(ChatUserSession.Create(connectionId, userId), cancellationToken);
        }

        /// <summary>
        /// Removes <paramref name="connectionId"/>'s session, returning the UserId it was logged in
        /// as only when this call is the one that actually removed it (null otherwise) — mirrors the
        /// old LoggedInUsers.TryRemove's role of letting an explicit Logout and an
        /// OnSubscriptionRemoved disconnect cleanup race safely, with only the call that actually
        /// removed a row publishing an offline notification.
        /// </summary>
        /// <remarks>
        /// Race note: the InMemory and MongoDB providers make the underlying delete atomic (a single
        /// locked dictionary removal, respectively a single-document Mongo command), so this
        /// guarantee is exact for them. The EntityFrameworkCore provider's generic DeleteAsync is a
        /// find-then-remove sequence across two separate scoped DbContexts for a genuinely concurrent
        /// Logout/disconnect race, which admits a narrow window where both calls could observe the
        /// row and both return non-null — an accepted, low-severity trade-off (an offline notification
        /// published twice, not a security or data-loss issue), not an oversight; see
        /// MongoDbChatContext's own comment on its transaction gap for the same kind of trade-off.
        /// </remarks>
        public async Task<Guid?> LogOutAsync(string connectionId, CancellationToken cancellationToken = default)
        {
            var existing = await chatContext.GetAsync<ChatUserSession>(session => session.ConnectionId == connectionId, cancellationToken);
            if (existing is null)
                return null;

            var removed = await chatContext.DeleteAsync<ChatUserSession, Guid>(existing.Id, cancellationToken);
            return removed ? existing.UserId : null;
        }

        /// <summary>
        /// The UserId <paramref name="connectionId"/> is currently logged in as, or null when it
        /// never logged in or was logged out. Replaces the old ChatChannel.TryGetLoggedInUserId's
        /// node-local dictionary lookup with a cluster-wide, persisted one — see ChatUserSession's
        /// own doc comment.
        /// </summary>
        public async Task<Guid?> GetUserIdAsync(string connectionId, CancellationToken cancellationToken = default)
        {
            var session = await chatContext.GetAsync<ChatUserSession>(existing => existing.ConnectionId == connectionId, cancellationToken);
            return session?.UserId;
        }

        /// <summary>
        /// Every distinct UserId with a currently-logged-in session, cluster-wide — the persisted
        /// equivalent of the old LoggedInUsers.Values.Distinct(). A user with more than one open
        /// connection still has more than one row here (one per connectionId) but appears once in
        /// this result, same as before.
        /// </summary>
        public async Task<IReadOnlyCollection<Guid>> GetOnlineUserIdsAsync(CancellationToken cancellationToken = default)
        {
            var sessions = await chatContext.GetAllAsync<ChatUserSession>(cancellationToken);
            return sessions.Select(session => session.UserId).Distinct().ToList();
        }
    }
}
