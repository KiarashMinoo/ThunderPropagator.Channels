using RapidStreamer.Application.Channels;
using RapidStreamer.Application.Channels.ChannelProgramsDescriptors;
using RapidStreamer.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using RapidStreamer.Application.Channels.Metadata;
using RapidStreamer.BuildingBlocks.Application.Enums;

namespace RapidStreamer.Channels.Notifications
{
    public
#if !DEBUG
        sealed
#endif
        class NotificationsChannelMetadata<TNotificationsChannelConfiguration> : AbstractChannelMetadata<NotificationsChannel<TNotificationsChannelConfiguration>>
        where TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()
    {
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors
            => new()
            {
                new SubscribingKeyChannelProgramsDescriptor(0, nameof(NotificationsChannelFeederMessage.UserId)).SetTable(nameof(NotificationsChannelFeederMessage.UserId)),
                new SubscribingKeyChannelProgramsDescriptor(1, nameof(NotificationsChannelFeederMessage.Date)).SetTable(nameof(NotificationsChannelFeederMessage.Date)),
                new ChannelProgramsDescriptor(2, nameof(NotificationsChannelFeederMessage.Id), DataType.String, "The identifier").SetTable(nameof(Notifications)),
                new TimeChannelProgramsDescriptor(3, nameof(NotificationsChannelFeederMessage.Time), "The time").SetTable(nameof(Notifications)),
                new ChannelProgramsDescriptor(4, nameof(NotificationsChannelFeederMessage.Origin), DataType.String, "The origin").SetTable(nameof(Notifications)),
                new EnumChannelProgramsDescriptor<NotificationType>(5, nameof(NotificationsChannelFeederMessage.Type), "The notification type").SetTable(nameof(Notifications)),
                new EnumChannelProgramsDescriptor<NotificationPriority>(6, nameof(NotificationsChannelFeederMessage.Priority), "The notification priority").SetTable(nameof(Notifications)),
                new ChannelProgramsDescriptor(7, nameof(NotificationsChannelFeederMessage.Icon), DataType.String, "The icon").SetTable(nameof(Notifications)),
                new ChannelProgramsDescriptor(8, nameof(NotificationsChannelFeederMessage.Subject), DataType.String, "The subject").SetTable(nameof(Notifications)),
                new ChannelProgramsDescriptor(9, nameof(NotificationsChannelFeederMessage.Body), DataType.String, "The body").SetTable(nameof(Notifications)),
                new ChannelProgramsDescriptor(10, nameof(NotificationsChannelFeederMessage.EllipsisBody), DataType.String, "The overflowed form of the body").SetTable(nameof(Notifications)),
                new NumberChannelProgramsDescriptor(11, nameof(NotificationsChannelFeederMessage.Seen), "The seen type(Bitwise`)").SetTable(nameof(Notifications)),
                new JsonChannelProgramsDescriptor(12, nameof(NotificationsChannelFeederMessage.Metadata), "The metadata").SetTable(nameof(Notifications))
            };
    }
}