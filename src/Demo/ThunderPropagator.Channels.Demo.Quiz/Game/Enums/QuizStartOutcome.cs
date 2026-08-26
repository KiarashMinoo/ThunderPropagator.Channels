using ThunderPropagator.Channels.Demo.Quiz.Channel;
namespace ThunderPropagator.Channels.Demo.Quiz.Game.Enums
{
    /// <summary>
    /// What happened to one call to <see cref="QuizChannel.StartGame"/> — part of
    /// <c>QuizStartGameReceiverPipeline</c>'s public response contract (#193).
    /// </summary>
    public enum QuizStartOutcome
    {
        /// <summary>This call actually transitioned the game out of Lobby and broadcast the new state.</summary>
        Started,

        /// <summary>
        /// The game had already left Lobby — either an earlier request from the same host, or a
        /// concurrent one that won the race (#193's own AC: "Concurrent requests create one running
        /// loop") — so this call made no change and broadcast nothing further.
        /// </summary>
        AlreadyStarted
    }
}
