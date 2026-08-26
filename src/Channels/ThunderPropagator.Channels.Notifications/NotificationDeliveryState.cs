using ThunderPropagator.Channels.Notifications.Messages;
namespace ThunderPropagator.Channels.Notifications
{
    /// <summary>
    /// Delivery/read lifecycle state of a notification, exposed through
    /// <see cref="NotificationsChannelFeederMessage.Seen"/>. Combinable via bitwise OR — a
    /// notification typically accumulates flags as it progresses (e.g.
    /// <see cref="Delivered"/> → <see cref="Delivered"/>|<see cref="Seen"/> →
    /// <see cref="Delivered"/>|<see cref="Seen"/>|<see cref="Read"/>), though this package doesn't
    /// enforce that progression itself — the setter only rejects bits outside the defined flags (see
    /// #70), not a specific transition order. Each flag has a stable, explicit numeric value; new
    /// flags added in the future will use the next unused power of two rather than reusing or
    /// renumbering these.
    /// </summary>
    [Flags]
    public enum NotificationDeliveryState
    {
        /// <summary>No delivery/read state recorded yet. The default when unset.</summary>
        None = 0,

        /// <summary>The notification has been delivered to the recipient's client.</summary>
        Delivered = 1,

        /// <summary>The recipient has seen the notification (e.g. it appeared in their feed/tray).</summary>
        Seen = 2,

        /// <summary>The recipient has opened/read the notification's content.</summary>
        Read = 4,

        /// <summary>The recipient has dismissed the notification.</summary>
        Dismissed = 8
    }
}
