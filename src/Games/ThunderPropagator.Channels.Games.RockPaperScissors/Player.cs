using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Channels.Games.RockPaperScissors.Messages;

namespace ThunderPropagator.Channels.Games.RockPaperScissors
{
    internal
#if !DEBUG
        sealed
#endif
        class Player
    {
        public Subscription? Subscription { get; }
        public string Name { get; }
        public PlayerType PlayerType { get; }
        public MoveKind Move { get; }

        /// <summary>
        /// The opponent type this player asked for when subscribing (Human or Computer) — read from the
        /// <see cref="RockPaperScissorsChannelFeederMessage.Opponent"/> subscribed key. Issue #12's own
        /// fix: the original matchmaking code checked this player's own <see cref="PlayerType"/> instead,
        /// which is always <see cref="RockPaperScissors.PlayerType.Human"/> for a real subscriber (a
        /// computer never subscribes) — so a request for a computer match could never actually be
        /// honored. Meaningless (defaults to Human, never read) for a synthetic computer-opponent
        /// <see cref="Player"/>.
        /// </summary>
        public PlayerType RequestedOpponent { get; }

        public Player(Subscription subscription)
            : this(
                subscription.SubscribedPrograms.SubscribedKeys[nameof(RockPaperScissorsChannelFeederMessage.PlayerName)],
                PlayerType.Human,
                Enum.Parse<MoveKind>(subscription.SubscribedPrograms.SubscribedKeys[nameof(RockPaperScissorsChannelFeederMessage.Move)]),
                Enum.Parse<PlayerType>(subscription.SubscribedPrograms.SubscribedKeys[nameof(RockPaperScissorsChannelFeederMessage.Opponent)]))
        {
            Subscription = subscription;
        }

        public Player(string name, PlayerType playerType, MoveKind move, PlayerType requestedOpponent = PlayerType.Human)
        {
            Name = name;
            PlayerType = playerType;
            Move = move;
            RequestedOpponent = requestedOpponent;
        }
    }
}
