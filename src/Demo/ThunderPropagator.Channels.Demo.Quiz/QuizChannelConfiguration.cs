using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    public
#if !DEBUG
        sealed
#endif
        class QuizChannelConfiguration : AbstractChannelConfiguration
    {
        public QuizFeederConfiguration FeederConfiguration { get; set; } = new();

        /// <summary>Maximum number of connected players a single game allows before <see cref="QuizChannel.Join"/> rejects a genuinely new one as <see cref="QuizGameFullException"/>. A reconnect or duplicate of an existing player is never rejected for this reason. Default: 8.</summary>
        public int MaxPlayers { get; set; } = 8;

        /// <summary>Whether <see cref="QuizChannel.Join"/> allows joining a game that has already left its Lobby phase. Default: <see langword="true"/> — a mid-game joiner receives the game's current state via the usual snapshot-replay-on-subscribe, per #187's own AC. Set to <see langword="false"/> to reject non-Lobby joins with <see cref="QuizNonLobbyJoinNotAllowedException"/> instead.</summary>
        public bool AllowMidGameJoin { get; set; } = true;

        public QuizChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}
