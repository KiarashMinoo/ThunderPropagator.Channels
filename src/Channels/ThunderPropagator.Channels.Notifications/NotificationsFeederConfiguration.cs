using Ardalis.GuardClauses;
using ThunderPropagator.Application.Feeders;

namespace ThunderPropagator.Channels.Notifications
{
    /// <summary>
    /// Base configuration for feeder implementations that push notifications into a
    /// <see cref="NotificationsChannel{TNotificationsChannelConfiguration}"/>. This package doesn't
    /// ship a feeder itself — Notifications is push-only, and a consumer registers their own
    /// <c>AbstractFeeder</c> subclass via one of the
    /// <c>NotificationsExtensions.AddNotificationsChannelFeeder</c> overloads with a concrete
    /// subclass of this type. The properties below are settings a feeder implementation is expected
    /// to read and act on (batching, deduplication, expiration, retry) — this configuration only
    /// stores and validates the values, it does not enforce them itself. Also carries the settings
    /// inherited from the base feeder configuration (enable/disable, timeouts, etc.).
    /// </summary>
    public abstract class NotificationsFeederConfiguration : AbstractFeederConfiguration
    {
        /// <summary>
        /// Maximum number of notifications a feeder implementation should accumulate before
        /// emitting them as a single batch. Default: 1, preserving today's one-at-a-time delivery.
        /// </summary>
        public int BatchSize
        {
            get => Get(1);
            set => Set(Guard.Against.NegativeOrZero(value, nameof(BatchSize)));
        }

        /// <summary>
        /// Time window within which a feeder implementation should treat two otherwise-identical
        /// notifications as duplicates and suppress the second. Default: <see cref="TimeSpan.Zero"/>
        /// (deduplication disabled), preserving today's behavior of delivering every notification
        /// as-is.
        /// </summary>
        public TimeSpan DeduplicationWindow
        {
            get => Get(TimeSpan.Zero);
            set => Set(Guard.Against.Negative(value, nameof(DeduplicationWindow)));
        }

        /// <summary>
        /// Default lifetime after which a feeder implementation should stop attempting to deliver a
        /// notification. This is a default only, not an enforced ceiling: a feeder implementation
        /// that supports a per-message expiry (e.g. reading it from the message's own payload) may
        /// let a specific message override it. Null means no expiration, preserving today's
        /// behavior.
        /// </summary>
        public TimeSpan? TimeToLive
        {
            // ServiceConfiguration.Get<T>'s string-backed conversion doesn't special-case
            // Nullable<TimeSpan> (only the non-nullable TimeSpan type name), so it throws
            // InvalidCastException when a bound/stored value is read back as TimeSpan?. Stored
            // internally as a plain TimeSpan with TimeSpan.Zero as the "unset" sentinel instead —
            // safe because zero is already rejected as an actual TTL below — and translated to/from
            // null at this property's public surface.
            get
            {
                var stored = Get(TimeSpan.Zero);
                return stored == TimeSpan.Zero ? null : stored;
            }
            set => Set(value is null ? TimeSpan.Zero : Guard.Against.NegativeOrZero(value.Value, nameof(TimeToLive)));
        }

        /// <summary>
        /// Maximum number of delivery attempts a feeder implementation should make for a single
        /// notification before giving up. Default: 1 (a single attempt, no retry), preserving
        /// today's behavior of not retrying failed deliveries.
        /// </summary>
        public int MaxDeliveryAttempts
        {
            get => Get(1);
            set => Set(Guard.Against.NegativeOrZero(value, nameof(MaxDeliveryAttempts)));
        }
    }
}