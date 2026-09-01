namespace ThunderPropagator.Channels.Chat.Models.Sessions
{
    // Issue #46: replaces ChatChannel's old in-memory LoggedInUsers dictionary. A row here means
    // "this connection is currently logged in as this user" — persisted through the same
    // multi-provider IChatContext as User/Group/Message, so presence is visible to every cluster
    // node instead of only the one the connection happens to be attached to. Sender identity itself
    // is resolved from the connection's own ClaimsPrincipal (see ChatChannelIdentity), not from this
    // table — this table exists only so "who's online" (ChatChannelGetOnlineUsersReceiverPipeline)
    // can be answered cluster-wide.
    public
#if !DEBUG
        sealed
#endif
        class ChatUserSession
    {
        public Guid Id { get; }
        public string ConnectionId { get; } = null!;
        public Guid UserId { get; }

        private ChatUserSession()
        {
            Id = Guid.NewGuid();
        }

        private ChatUserSession(string connectionId, Guid userId) : this()
        {
            ConnectionId = connectionId;
            UserId = userId;
        }

        internal static ChatUserSession Create(string connectionId, Guid userId) => new(connectionId, userId);
    }
}
