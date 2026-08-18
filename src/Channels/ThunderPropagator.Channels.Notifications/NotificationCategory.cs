namespace ThunderPropagator.Channels.Notifications
{
    /// <summary>
    /// Semantic meaning of a notification (what it's about), independent of
    /// <see cref="NotificationContentType"/> (how its body is formatted) and
    /// <see cref="NotificationPriority"/> (how urgent it is) — see #69. Purely descriptive: this
    /// package doesn't change routing, delivery, or storage based on this value; a consuming client
    /// may use it to choose an icon, color, or grouping.
    /// </summary>
    /// <example>
    /// Selecting each property independently — an HTML-formatted, high-priority error notification:
    /// <code>
    /// var message = new NotificationsChannelFeederMessage
    /// {
    ///     Id = "order-failed-42",
    ///     Subject = "Order #42 failed",
    ///     Body = "&lt;p&gt;We couldn't process your payment.&lt;/p&gt;",
    ///     Type = NotificationContentType.Html,   // how the Body is formatted
    ///     Category = NotificationCategory.Error, // what the notification is about
    ///     Priority = NotificationPriority.High    // how urgent it is
    /// };
    /// </code>
    /// </example>
    public enum NotificationCategory
    {
        /// <summary>General informational notification. The default when unset.</summary>
        Info,

        /// <summary>Confirms a successful outcome.</summary>
        Success,

        /// <summary>Warns of a potential problem that isn't yet an error.</summary>
        Warning,

        /// <summary>Reports a failure or error condition.</summary>
        Error,

        /// <summary>Time-sensitive notification requiring the recipient's attention.</summary>
        Alert,

        /// <summary>Notification originating from the system itself rather than user activity.</summary>
        System
    }
}
