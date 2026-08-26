using ThunderPropagator.Channels.Demo.Quiz.Messages;
namespace ThunderPropagator.Channels.Demo.Quiz.Game
{
    /// <summary>
    /// The outcome of <see cref="QuizGameSession.Join"/>: the resulting <see cref="QuizPlayer"/>
    /// membership, whether this call reconnected an existing player rather than adding a new one, and
    /// the session's current public state at the moment of joining — the single snapshot a caller
    /// (a future join pipeline, #191) unicasts to the joining connection alone. A caller must not
    /// rebroadcast <see cref="CurrentState"/> to the session's other players — every other connection
    /// either already observed the state this snapshot represents, or will observe it fresh the next
    /// time it changes.
    /// </summary>
    internal sealed record QuizPlayerJoinResult(QuizPlayer Player, bool IsReconnect, QuizChannelFeederMessage? CurrentState);
}
