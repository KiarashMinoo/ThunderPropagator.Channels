using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Metadata;
using ThunderPropagator.BuildingBlocks.Application.Enums;

namespace ThunderPropagator.Channels.Notifications
{
    /// <summary>Channel metadata for <see cref="NotificationsChannel{TNotificationsChannelConfiguration}"/>.</summary>
    public
#if !DEBUG
        sealed
#endif
        class NotificationsChannelMetadata<TNotificationsChannelConfiguration> : AbstractChannelMetadata<NotificationsChannel<TNotificationsChannelConfiguration>>
        where TNotificationsChannelConfiguration : AbstractChannelConfiguration, new()
    {
        /// <summary>
        /// Declares <see cref="NotificationsChannelFeederMessage"/>'s field schema for subscription,
        /// routing, and snapshot purposes. <see cref="NotificationsChannelFeederMessage.UserId"/> is
        /// the only subscribing key (see #61) — a client subscribes by UserId alone and does not
        /// need to know a notification's <see cref="NotificationsChannelFeederMessage.Date"/> or
        /// <see cref="NotificationsChannelFeederMessage.Id"/> in advance. Every other field is
        /// declared here as a regular (non-subscribing) field so it's available for snapshot storage
        /// and, via <c>NotificationsChannel.SearchHistoricalNotificationsAsync</c>, optional
        /// historical filtering.
        /// </summary>
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors
            => new()
            {
                new SubscribingKeyChannelProgramsDescriptor(0, nameof(NotificationsChannelFeederMessage.UserId)).SetTable(nameof(NotificationsChannelFeederMessage.UserId)),
                new DateTimeChannelProgramsDescriptor(1, nameof(NotificationsChannelFeederMessage.Date), "The notification date, usable as an optional historical filter").SetTable(nameof(NotificationsChannelFeederMessage.Date)),
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