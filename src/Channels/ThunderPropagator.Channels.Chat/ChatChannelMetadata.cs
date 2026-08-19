using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Metadata;
using ThunderPropagator.BuildingBlocks.Application.Enums;

namespace ThunderPropagator.Channels.Chat
{
    public
#if !DEBUG
        sealed
#endif
        class ChatChannelMetadata : AbstractChannelMetadata<ChatChannel>
    {
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors => new()
        {
            new SubscribingKeyChannelProgramsDescriptor(0, nameof(ChatChannelFeederMessage.UserId)),
            new ChannelProgramsDescriptor(1, nameof(ChatChannelFeederMessage.SenderUserId), DataType.String),
            new DateTimeChannelProgramsDescriptor(2, nameof(ChatChannelFeederMessage.DateTime), "The UTC timestamp the message was created"),
            new ChannelProgramsDescriptor(3, nameof(ChatChannelFeederMessage.GroupId), DataType.String),
            new ChannelProgramsDescriptor(4, nameof(ChatChannelFeederMessage.Message), DataType.String)
        };
    }
}