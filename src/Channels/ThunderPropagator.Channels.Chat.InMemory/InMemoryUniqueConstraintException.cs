using ThunderPropagator.Channels.Chat.InMemory.Context;
namespace ThunderPropagator.Channels.Chat.InMemory
{
    /// <summary>
    /// Thrown by <see cref="InMemoryChatStore"/> when a create/update would violate one of the same
    /// uniqueness rules the persistent providers enforce at the database level (#110's
    /// UserConfiguration/GroupUserConfiguration, #111's index definitions): a duplicate
    /// <c>User.UserName</c>, or a duplicate <c>(GroupId, UserId)</c> group membership.
    /// </summary>
    public
#if !DEBUG
        sealed
#endif
        class InMemoryUniqueConstraintException(string message) : Exception(message)
    {
    }
}
