namespace ThunderPropagator.Channels.Notifications
{
    /// <summary>
    /// Optional date-range filter for historical notification queries, passed to
    /// NotificationsChannel.SearchHistoricalNotificationsAsync. Unrelated to live subscription
    /// identity, which is keyed by UserId alone. Leaving both bounds null returns the recipient's
    /// full history.
    /// </summary>
    public sealed class NotificationsHistoricalDateRangeFilter(DateTime? from = null, DateTime? to = null)
    {
        public DateTime? From { get; } = from;
        public DateTime? To { get; } = to;

        public bool IsSatisfiedBy(DateTime date) => (From is null || date >= From) && (To is null || date <= To);
    }
}
