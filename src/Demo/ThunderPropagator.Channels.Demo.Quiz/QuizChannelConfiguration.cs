using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    public
#if !DEBUG
        sealed
#endif
        class QuizChannelConfiguration : AbstractChannelConfiguration
    {
        private int _maxPlayers = 8;
        private int _minPlayers = 2;

        public QuizFeederConfiguration FeederConfiguration { get; set; } = new();

        /// <summary>Maximum number of connected players a single game allows before <see cref="QuizChannel.Join"/> rejects a genuinely new one as <see cref="QuizGameFullException"/>. A reconnect or duplicate of an existing player is never rejected for this reason. Must be strictly positive. Default: 8.</summary>
        public int MaxPlayers
        {
            get => _maxPlayers;
            set => _maxPlayers = ValidatePositive(value, nameof(MaxPlayers));
        }

        /// <summary>Minimum number of connected players required before <see cref="QuizChannel.StartGame"/> allows the host to start — rejected as <see cref="QuizNotEnoughPlayersException"/> otherwise. Must be strictly positive, and — enforced separately, once both properties are known, by <see cref="QuizChannelExtensions.AddQuizChannel"/> itself rather than by either setter alone — no greater than <see cref="MaxPlayers"/>. Default: 2 (a quiz needs at least two players to be meaningfully competitive).</summary>
        public int MinPlayers
        {
            get => _minPlayers;
            set => _minPlayers = ValidatePositive(value, nameof(MinPlayers));
        }

        /// <summary>Whether <see cref="QuizChannel.Join"/> allows joining a game that has already left its Lobby phase. Default: <see langword="true"/> — a mid-game joiner receives the game's current state via the usual snapshot-replay-on-subscribe, per #187's own AC. Set to <see langword="false"/> to reject non-Lobby joins with <see cref="QuizNonLobbyJoinNotAllowedException"/> instead.</summary>
        public bool AllowMidGameJoin { get; set; } = true;

        public QuizChannelConfiguration()
        {
            IsEnabled = true;
        }

        private static int ValidatePositive(int value, string propertyName)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0, propertyName);
            return value;
        }
    }
}
