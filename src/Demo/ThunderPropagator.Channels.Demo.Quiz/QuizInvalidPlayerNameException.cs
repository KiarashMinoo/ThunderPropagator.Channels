using ThunderPropagator.Channels.Demo.Quiz.Channel;
using ThunderPropagator.Channels.Demo.Quiz.Messages;
namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// Thrown by <see cref="QuizChannel.Join"/> when a display name fails validation after
    /// normalization (see <see cref="QuizChannel"/>'s own remarks on trimming/collapsing whitespace) —
    /// currently only for exceeding <see cref="QuizChannelFeederMessage.TextMaxLength"/>, the same
    /// bound the game's own broadcast scoreboard enforces on every entry's PlayerName.
    /// </summary>
    public sealed class QuizInvalidPlayerNameException(string playerName, string rule) : Exception($"Invalid player name '{playerName}': {rule}")
    {
        public string PlayerName { get; } = playerName;
    }
}
