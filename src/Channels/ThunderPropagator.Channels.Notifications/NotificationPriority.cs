namespace ThunderPropagator.Channels.Notifications
{
    /// <summary>
    /// Relative importance of a notification. Purely descriptive — this package does not change
    /// routing, delivery order, or retry behavior based on this value; a consuming client or a
    /// feeder implementation (see <see cref="NotificationsFeederConfiguration"/>) may use it to sort,
    /// filter, or apply visual treatment. The underlying values are ordered (<see cref="VeryLow"/> is
    /// the lowest) so ordinary numeric comparison reflects relative priority.
    /// </summary>
    public enum NotificationPriority
    {
        /// <summary>Lowest priority.</summary>
        VeryLow = -2,

        /// <summary>Below-normal priority.</summary>
        Low,

        /// <summary>Ordinary priority. The default when unset.</summary>
        Normal,

        /// <summary>Above-normal priority.</summary>
        High,

        /// <summary>Highest priority.</summary>
        VeryHigh,
    }
}