using Bogus;
using ThunderPropagator.Channels.Games.RockPaperScissors.Channel;
using ThunderPropagator.Channels.Games.RockPaperScissors.Messages;

namespace ThunderPropagator.Channels.Games.RockPaperScissors
{
    internal
#if !DEBUG
        sealed
#endif
        class RockPaperScissorsComputer
    {
        private readonly RockPaperScissorsChannel _channel;

        public RockPaperScissorsComputer(RockPaperScissorsChannel channel)
        {
            _channel = channel;
        }

        // -1 = moveKind beats compareTo, 0 = draw, 1 = moveKind loses to compareTo. Verified against
        // real Rock-Paper-Scissors rules for all six non-draw pairs (Rock beats Scissor, Scissor beats
        // Paper, Paper beats Rock, and their reverses).
        private static int CompareTo(MoveKind moveKind, MoveKind compareTo) => moveKind switch
        {
            MoveKind.Paper => compareTo switch
            {
                MoveKind.Rock => -1,
                MoveKind.Scissor => 1,
                _ => 0
            },
            MoveKind.Rock => compareTo switch
            {
                MoveKind.Scissor => -1,
                MoveKind.Paper => 1,
                _ => 0
            },
            MoveKind.Scissor => compareTo switch
            {
                MoveKind.Paper => -1,
                MoveKind.Rock => 1,
                _ => 0
            },
            _ => 0
        };

        private static MoveKind Move()
        {
            var array = Enum.GetValues<MoveKind>().Cast<int>().ToArray();
            // Issue #12's own fix: Random.Shared.Next's own upper bound is exclusive, so the previous
            // `Next(0, array.Length - 1)` could only ever return index 0 or 1 (Rock or Paper) — the
            // computer could never actually pick Scissor (index 2).
            var random = Random.Shared.Next(0, array.Length);
            return (MoveKind)array[random];
        }

        /// <summary>
        /// The single entry point <see cref="RockPaperScissorsChannelReceiveEvent"/> calls once a new
        /// subscription is registered — issue #12's own scope, "keep a session for the game and push
        /// notification to all of them": resolves the subscribing connection's own
        /// <see cref="Player"/>, routes to a computer or human match per that player's own
        /// <see cref="RockPaperScissors.Player.RequestedOpponent"/> (issue #12's own fix — see that
        /// property's own remarks for why the original code's routing check could never actually select
        /// a computer match), and lets <see cref="Play"/> record the session and push results. A no-op if
        /// <paramref name="connectionId"/> is not (or no longer) a subscriber — e.g. a receive event
        /// racing a near-simultaneous unsubscribe.
        /// </summary>
        public void HandleSubscription(string connectionId)
        {
            var subscription = _channel.FindSubscription(connectionId);
            if (subscription is null)
                return;

            var player = new Player(subscription);

            if (player.RequestedOpponent == PlayerType.Computer)
                PlayWithComputer(player);
            else
                PlayWithHuman(player);
        }

        internal void PlayWithComputer(Player player) =>
            Play(player, new Player(new Person().FullName, PlayerType.Computer, Move()));

        internal void PlayWithHuman(Player player)
        {
            var opponentSubscription = _channel.PeekRandomPlayer(player.Subscription?.ConnectionInfo.ConnectionId);
            if (opponentSubscription is null)
                return;

            Play(player, new Player(opponentSubscription));
        }

        private void Play(Player firstPlayer, Player secondPlayer)
        {
            // Issue #12's own fix: this must call the RPS-aware static CompareTo above, not
            // firstPlayer.Move.CompareTo(secondPlayer.Move) (the original code's own call, which resolves
            // to MoveKind's built-in enum comparison — comparing the enum's underlying int values 1/2/3 —
            // and has nothing to do with who actually wins a round).
            var winningValue = CompareTo(firstPlayer.Move, secondPlayer.Move);

            _channel.RecordSession(firstPlayer, secondPlayer);

            SendPlayResponse(firstPlayer, secondPlayer, winningValue == -1, winningValue == 0);

            // A synthetic computer opponent has no real subscriber/connection to notify.
            if (secondPlayer.PlayerType != PlayerType.Computer)
                SendPlayResponse(secondPlayer, firstPlayer, winningValue == 1, winningValue == 0);
        }

        private void SendPlayResponse(Player recipient, Player opponent, bool isWin, bool isDraw)
        {
            // PlayerName/Opponent/Move must echo the recipient's own original subscribed keys exactly
            // (see RockPaperScissorsChannelMetadata's own ChannelProgramsDescriptors) for
            // RockPaperScissorsChannel.PushResult's key-based routing to deliver this to them, and only
            // them.
            var message = new RockPaperScissorsChannelFeederMessage
            {
                PlayerName = recipient.Name,
                Opponent = recipient.RequestedOpponent,
                Move = recipient.Move,
                OpponentName = opponent.Name,
                OpponentMove = opponent.Move,
                IsWin = isWin,
                IsDraw = isDraw
            };

            _channel.PushResult(message);
        }
    }
}
