using Bogus;
using RapidStreamer.Application.Channels.Subscribers;

namespace RapidStreamer.Channels.Games.RockPaperScissors
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

        //-1,0,1
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
            var random = Random.Shared.Next(0, array.Length - 1);
            return (MoveKind)array[random];
        }

        private async Task Play(Player firstPlayer, Player secondPlayer, CancellationToken cancellationToken = default)
        {
            var winningValue = firstPlayer.Move.CompareTo(secondPlayer.Move);

            await SendPlayResponseAsync(firstPlayer.Subscription!, secondPlayer, winningValue == -1, winningValue == 0);
            if (secondPlayer.PlayerType != PlayerType.Computer)
            {
                await SendPlayResponseAsync(secondPlayer.Subscription!, firstPlayer, winningValue == 1, winningValue == 0);
            }

            return;

            async Task SendPlayResponseAsync(Subscription subscription, Player opponent, bool isWin, bool isDraw)
            {
                var firstPlayerMessage = new RockPaperScissorsChannelFeederMessage
                {
                    OpponentName = opponent.Name,
                    OpponentMove = opponent.Move,
                    IsWin = isWin,
                    IsDraw = isDraw
                };
                await _channel.SendAsync(subscription, firstPlayerMessage, cancellationToken: cancellationToken);
            }
        }

        public Task PlayWithComputer(Player player, CancellationToken cancellationToken = default)
        {
            return Play(player,
                new Player(new Person().FullName, PlayerType.Computer, Move()),
                cancellationToken);
        }

        public async Task PlayWithHuman(Player player, CancellationToken cancellationToken = default)
        {
            var opponentSubscription = _channel.PeekRandomPlayer();
            if (opponentSubscription is not null)
            {
                var opponent = new Player(opponentSubscription);
                await Play(player, opponent, cancellationToken);
            }
        }
    }
}