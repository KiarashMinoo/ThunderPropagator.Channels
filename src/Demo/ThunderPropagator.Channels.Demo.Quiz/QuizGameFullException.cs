using ThunderPropagator.Channels.Demo.Quiz.Channel;
using ThunderPropagator.Channels.Demo.Quiz.Configuration;
namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// Thrown by <see cref="QuizChannel.Join"/> when a genuinely new player (not a reconnect or
    /// duplicate of an existing one, who never need a fresh seat) would push a game past
    /// <see cref="QuizChannelConfiguration.MaxPlayers"/> connected players.
    /// </summary>
    public sealed class QuizGameFullException(string gameId, int maxPlayers) : Exception($"Game '{gameId}' already has the maximum of {maxPlayers} connected player(s).")
    {
        public string GameId { get; } = gameId;
        public int MaxPlayers { get; } = maxPlayers;
    }
}
