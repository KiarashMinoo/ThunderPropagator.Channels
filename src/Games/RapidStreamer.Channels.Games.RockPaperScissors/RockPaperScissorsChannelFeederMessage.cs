using RapidStreamer.BuildingBlocks.Application;

namespace RapidStreamer.Channels.Games.RockPaperScissors
{
    internal
#if !DEBUG
        sealed
#endif
        class RockPaperScissorsChannelFeederMessage : FeederMessage
    {
        //Player
        public string PlayerName
        {
            get => GetValueOrDefault(string.Empty);
            internal set => SetValue(value);
        }

        public PlayerType Opponent
        {
            get => GetValueOrDefault(PlayerType.Human);
            internal set => SetValue(value);
        }

        public MoveKind Move
        {
            get => GetValueOrDefault(MoveKind.Rock);
            internal set => SetValue(value);
        }

        //Opponent
        public string OpponentName
        {
            get => GetValueOrDefault(string.Empty);
            internal set => SetValue(value);
        }

        public MoveKind OpponentMove
        {
            get => GetValueOrDefault(MoveKind.Rock);
            internal set => SetValue(value);
        }

        //Status
        public bool IsDraw
        {
            get => GetValueOrDefault(false);
            internal set => SetValue(value);
        }

        public bool IsWin
        {
            get => GetValueOrDefault(false);
            internal set => SetValue(value);
        }
    }
}