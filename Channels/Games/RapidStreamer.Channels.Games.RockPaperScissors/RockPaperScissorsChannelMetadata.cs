using RapidStreamer.Application.Channels.ChannelProgramsDescriptors;
using RapidStreamer.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using RapidStreamer.Application.Channels.Metadata;
using RapidStreamer.BuildingBlocks.Application.Enums;

namespace RapidStreamer.Channels.Games.RockPaperScissors
{
    public
#if !DEBUG
        sealed
#endif
        class RockPaperScissorsChannelMetadata : AbstractChannelMetadata<RockPaperScissorsChannel>
    {
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors
            => new()
            {
                new SubscribingKeyChannelProgramsDescriptor(0, nameof(RockPaperScissorsChannelFeederMessage.PlayerName)),
                new SubscribingKeyChannelProgramsDescriptor(1, nameof(RockPaperScissorsChannelFeederMessage.Opponent)),
                new SubscribingKeyChannelProgramsDescriptor(2, nameof(RockPaperScissorsChannelFeederMessage.Move)),
                new ChannelProgramsDescriptor(3, nameof(RockPaperScissorsChannelFeederMessage.OpponentName), DataType.String, "the opponent name"),
                new EnumChannelProgramsDescriptor<MoveKind>(4, nameof(RockPaperScissorsChannelFeederMessage.OpponentMove)),
                new BooleanChannelProgramsDescriptor(5, nameof(RockPaperScissorsChannelFeederMessage.IsDraw), "Notifies when game has been drawn"),
                new BooleanChannelProgramsDescriptor(6, nameof(RockPaperScissorsChannelFeederMessage.IsWin), "Notifies when player has been won")
            };
    }
}