using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;

namespace ThunderPropagator.Channels.Games.TicTacToe
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
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public int Column
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public PlayerSign Sign
        {
            get => GetValueOrDefault(PlayerSign.X);
            set => SetValue(value);
        }
    }
}