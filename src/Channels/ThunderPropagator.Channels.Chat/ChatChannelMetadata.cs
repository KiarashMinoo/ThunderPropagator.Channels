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
            new ChannelProgramsDescriptor(4, nameof(ChatChannelFeederMessage.Message), DataType.String),
            // Issue #119: MessageId identifies which message a "send", "delete", or "edit" event
            // refers to; IsDeleted/IsEdited tell the three apart — both false for a newly sent
            // message, IsDeleted for a deletion, IsEdited for a revision (Message already carries
            // the revised body).
            new ChannelProgramsDescriptor(5, nameof(ChatChannelFeederMessage.MessageId), DataType.String),
            new BooleanChannelProgramsDescriptor(6, nameof(ChatChannelFeederMessage.IsDeleted), "Whether this event is a message deletion rather than a new message"),
            new BooleanChannelProgramsDescriptor(7, nameof(ChatChannelFeederMessage.IsEdited), "Whether this event is a message edit rather than a new message"),
            // Issue #121: a presence event (SenderUserId went offline) has no backing message at
            // all — MessageId/GroupId/Message/DateTime are left at their defaults for it.
            new BooleanChannelProgramsDescriptor(8, nameof(ChatChannelFeederMessage.IsOffline), "Whether this event is a presence notification rather than a message")
        };
    }
}