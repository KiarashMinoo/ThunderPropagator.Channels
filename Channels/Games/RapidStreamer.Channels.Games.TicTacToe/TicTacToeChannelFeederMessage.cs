using RapidStreamer.BuildingBlocks.Application;
using RapidStreamer.Channels.Games.TicTacToe.Game.Enums;

namespace RapidStreamer.Channels.Games.TicTacToe
{
    internal
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelFeederMessage : FeederMessage
    {
        public string SessionId
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
        }

        public string PlayerName
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
        }

        public int Row
        {
            get => GetValue<int>();
            set => SetValue(value);
        }

        public int Column
        {
            get => GetValue<int>();
            set => SetValue(value);
        }

        public PlayerSign Sign
        {
            get => GetValue<PlayerSign>();
            set => SetValue(value);
        }
    }
}