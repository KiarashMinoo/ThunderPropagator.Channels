using ThunderPropagator.Channels.Notifications.Messages;
namespace ThunderPropagator.Channels.Notifications
{
    /// <summary>
    /// Who a notification is routed to (see #76) — explicit in place of the previous implicit rule
    /// of inferring broadcast delivery from a null <see cref="NotificationsChannelFeederMessage.UserId"/>.
    /// <see cref="NotificationsChannelFeederMessage.ValidateAudienceCombination"/> enforces which of
    /// <see cref="NotificationsChannelFeederMessage.UserId"/>/<see cref="NotificationsChannelFeederMessage.GroupId"/>
    /// each value requires or forbids.
    /// </summary>
    public enum NotificationAudience
    {
        /// <summary>
        /// A single named recipient. The default when unset — the most restrictive value, so a
        /// message that never sets Audience can't accidentally reach anyone but the caller-specified
        /// recipient. Requires <see cref="NotificationsChannelFeederMessage.UserId"/>; forbids
        /// <see cref="NotificationsChannelFeederMessage.GroupId"/>.
        /// </summary>
        Individual,

        /// <summary>
        /// Every recipient already known to be a member of a specific group (see the channel's
        /// group fan-out, #74). Requires <see cref="NotificationsChannelFeederMessage.GroupId"/>;
        /// forbids <see cref="NotificationsChannelFeederMessage.UserId"/>.
        /// </summary>
        Group,

        /// <summary>
        /// Every current subscriber, with no group scoping. Forbids both
        /// <see cref="NotificationsChannelFeederMessage.UserId"/> and
        /// <see cref="NotificationsChannelFeederMessage.GroupId"/> — a broadcast never carries a
        /// leftover recipient or group identifier from a message it was copied or reused from.
        /// </summary>
        Broadcast
    }
}
