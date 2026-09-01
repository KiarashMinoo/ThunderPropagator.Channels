using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Metadata;
using ThunderPropagator.BuildingBlocks.Application.Enums;
using ThunderPropagator.Channels.Chat.Channel;
using ThunderPropagator.Channels.Chat.Messages;

namespace ThunderPropagator.Channels.Chat.Metadata
{
    public
#if !DEBUG
        sealed
#endif
        class ChatChannelMetadata : AbstractChannelMetadata<ChatChannel>
    {
        // Issue #34: enables the framework's own transport-level authentication gate, which this
        // metadata class never turned on (defaults to disabled). AuthenticationType.None rather than
        // Basic/OAuth2 — Chat's real authentication is entirely application-layer (UserService.LoginAsync,
        // tracked via the persisted ChatUserSessionService — see #46 — enforced per-pipeline by
        // AuthenticatedChatChannelReceiverPipeline — see #109), not one of this framework's own
        // transport-level credential schemes.
        public ChatChannelMetadata()
        {
            SetChannelAuthentication(true, AuthenticationType.None, null, null, null, 0);
        }

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
            new BooleanChannelProgramsDescriptor(8, nameof(ChatChannelFeederMessage.IsOffline), "Whether this event is a presence notification rather than a message"),
            // Issue #124: a group-deletion event has no backing message either — GroupId identifies
            // which group was deleted, SenderUserId who deleted it.
            new BooleanChannelProgramsDescriptor(9, nameof(ChatChannelFeederMessage.IsGroupDeleted), "Whether this event is a group deletion notification rather than a message"),
            // Issue #125: a read-receipt event reuses the Send/Delete/Edit shape, but UserId/
            // SenderUserId are swapped — see ChatChannelFeederMessage's own constructor comment.
            new BooleanChannelProgramsDescriptor(10, nameof(ChatChannelFeederMessage.IsRead), "Whether this event is a read receipt rather than a new message")
        };
    }
}