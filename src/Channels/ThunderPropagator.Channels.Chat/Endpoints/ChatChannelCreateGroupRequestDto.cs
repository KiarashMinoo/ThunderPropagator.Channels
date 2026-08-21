namespace ThunderPropagator.Channels.Chat.Endpoints
{
    // Issue #136: UserIds are the members to invite — the creator only ever comes from the
    // authenticated principal (see CreateGroupAsync), the same "no field a client could set
    // instead" shape #133's send request uses for the sender, and is not implicitly one of
    // UserIds; include the caller's own id here too if they should also be a member.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelCreateGroupRequestDto
    {
        public required string Name { get; init; }
        public Guid[]? UserIds { get; init; }
    }
}
