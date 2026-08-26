using ThunderPropagator.Channels.Demo.Quiz.Channel;
namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// Thrown by <see cref="QuizChannel.Join"/> when <see cref="GameId"/> matches no session in the
    /// channel's <see cref="Game.QuizGameSessionStore"/> — sessions are only ever created by the game
    /// loop itself (#189), never implicitly by a join, so a caller supplying an unknown GameId gets a
    /// clear rejection rather than silently starting an empty game nobody else is watching.
    /// </summary>
    public sealed class QuizGameNotFoundException(string gameId) : Exception($"No quiz game with GameId '{gameId}' exists.")
    {
        public string GameId { get; } = gameId;
    }
}
