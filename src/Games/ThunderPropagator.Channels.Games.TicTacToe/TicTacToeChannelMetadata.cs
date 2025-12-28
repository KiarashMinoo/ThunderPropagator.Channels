using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Metadata;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;

namespace ThunderPropagator.Channels.Games.TicTacToe
{
    public
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelMetadata : AbstractChannelMetadata<TicTacToeChannel>
    {
        public const string TicTacToeGame = nameof(TicTacToeGame);
        public const string TicTacToeGamePlayer = nameof(TicTacToeGamePlayer);

        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors => new()
        {
            new SubscribingKeyChannelProgramsDescriptor(0, nameof(TicTacToeChannelFeederMessage.SessionId)).SetTable(TicTacToeGame),
            new SubscribingKeyChannelProgramsDescriptor(1, nameof(TicTacToeChannelFeederMessage.PlayerName)).SetTable(TicTacToeGamePlayer),
            new EnumChannelProgramsDescriptor<PlayerSign>(2, nameof(TicTacToeChannelFeederMessage.Sign)).SetTable(TicTacToeGamePlayer),
            new NumberChannelProgramsDescriptor(3, nameof(TicTacToeChannelFeederMessage.Row)).SetTable(TicTacToeGamePlayer),
            new NumberChannelProgramsDescriptor(4, nameof(TicTacToeChannelFeederMessage.Column)).SetTable(TicTacToeGamePlayer)
        };
    }
}