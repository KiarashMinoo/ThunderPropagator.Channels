using RapidStreamer.Application.Channels.ChannelProgramsDescriptors;
using RapidStreamer.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using RapidStreamer.Application.Channels.Metadata;
using RapidStreamer.BuildingBlocks.Application.Enums;

namespace RapidStreamer.Channels.Chat
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
            new ChannelProgramsDescriptor(3, nameof(ChatChannelFeederMessage.GroupId), DataType.String),
            new ChannelProgramsDescriptor(4, nameof(ChatChannelFeederMessage.Message), DataType.String)
        };
    }
}