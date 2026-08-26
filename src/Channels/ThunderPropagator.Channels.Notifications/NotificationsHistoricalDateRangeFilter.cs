using ThunderPropagator.Channels.Notifications.Channel;
using ThunderPropagator.Channels.Notifications.Messages;
namespace ThunderPropagator.Channels.Notifications
{
    /// <summary>
    /// Optional date-range filter for historical notification queries, passed to
    /// NotificationsChannel.SearchHistoricalNotificationsAsync. Unrelated to live subscription
    /// identity, which is keyed by UserId alone. Leaving both bounds null returns the recipient's
    /// full history.
    /// </summary>
    /// <param name="from">Inclusive lower bound. Null means no lower bound.</param>
    /// <param name="to">Inclusive upper bound. Null means no upper bound.</param>
    public sealed class NotificationsHistoricalDateRangeFilter(DateTime? from = null, DateTime? to = null)
    {
        /// <summary>Inclusive lower bound of the range. Null means no lower bound.</summary>
        public DateTime? From { get; } = from;

        /// <summary>Inclusive upper bound of the range. Null means no upper bound.</summary>
        public DateTime? To { get; } = to;

        /// <summary>
        /// True if <paramref name="date"/> falls within <see cref="From"/> and <see cref="To"/>
        /// (both inclusive, either or both may be unset). Compares <paramref name="date"/> as given —
        /// callers are responsible for using a consistent <see cref="DateTimeKind"/> (this package
        /// always compares against UTC <see cref="NotificationsChannelFeederMessage.Date"/> values).
        /// </summary>
        public bool IsSatisfiedBy(DateTime date) => (From is null || date >= From) && (To is null || date <= To);
    }
}
