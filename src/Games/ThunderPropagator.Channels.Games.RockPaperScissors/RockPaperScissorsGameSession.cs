namespace ThunderPropagator.Channels.Games.RockPaperScissors
{
    /// <summary>
    /// One resolved Rock-Paper-Scissors match between two players (or a player and a synthetic computer
    /// opponent) — issue #12's own scope, "keep a session for the game." Kept purely server-side, never
    /// exposed on the wire (<see cref="Messages.RockPaperScissorsChannelFeederMessage"/> and its own
    /// <see cref="Metadata.RockPaperScissorsChannelMetadata.ChannelProgramsDescriptors"/> carry no
    /// SessionId field, and this ticket does not add one to that established wire protocol), so a
    /// completed match is never silently lost and so
    /// <see cref="Channel.RockPaperScissorsChannel.PeekRandomPlayer"/> can exclude a player who has
    /// already played from ever being handed out as a second player's own opponent again.
    /// </summary>
    internal sealed class RockPaperScissorsGameSession
    {
        public required string SessionId { get; init; }
        public required Player FirstPlayer { get; init; }
        public required Player SecondPlayer { get; init; }
        public required DateTimeOffset PlayedAt { get; init; }
    }
}
