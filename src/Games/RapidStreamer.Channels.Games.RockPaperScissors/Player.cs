using RapidStreamer.Application.Channels.Subscribers;

namespace RapidStreamer.Channels.Games.RockPaperScissors
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

        public Player(Subscription subscription)
            : this(subscription.SubscribedPrograms.SubscribedKeys[nameof(RockPaperScissorsChannelFeederMessage.PlayerName)],
                PlayerType.Human,
                Enum.Parse<MoveKind>(subscription.SubscribedPrograms.SubscribedKeys[nameof(RockPaperScissorsChannelFeederMessage.Move)]))
        {
            Subscription = subscription;
        }

        public Player(string name, PlayerType playerType, MoveKind move)
        {
            Name = name;
            PlayerType = playerType;
            Move = move;
        }
    }
}