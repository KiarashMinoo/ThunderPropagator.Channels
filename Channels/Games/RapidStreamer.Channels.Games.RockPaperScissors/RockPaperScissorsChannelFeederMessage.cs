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
            get => GetValue<PlayerType>();
            internal set => SetValue(value);
        }

        public MoveKind Move
        {
            get => GetValue<MoveKind>();
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
            get => GetValue<MoveKind>();
            internal set => SetValue(value);
        }

        //Status
        public bool IsDraw
        {
            get => GetValue<bool>();
            internal set => SetValue(value);
        }

        public bool IsWin
        {
            get => GetValue<bool>();
            internal set => SetValue(value);
        }
    }
}