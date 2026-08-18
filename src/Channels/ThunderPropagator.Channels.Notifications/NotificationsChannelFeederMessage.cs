using System.Globalization;
using Ardalis.GuardClauses;
using ThunderPropagator.BuildingBlocks.Application;

namespace ThunderPropagator.Channels.Notifications
{
    public
#if !DEBUG
        sealed
#endif
        class NotificationsChannelFeederMessage : FeederMessage
    {
        /// <summary>
        /// Maximum number of text elements (user-perceived characters — see
        /// <see cref="StringInfo"/> — rather than raw UTF-16 code units, so a truncation never
        /// splits a surrogate pair or combining character sequence) that <see cref="EllipsisBody"/>
        /// keeps from <see cref="Body"/> before appending <see cref="EllipsisSuffix"/>. Chosen as a
        /// reasonable notification-list preview length.
        /// </summary>
        public const int EllipsisBodyThreshold = 100;

        private const string EllipsisSuffix = "...";

        public NotificationsChannelFeederMessage()
        {
        }

        internal NotificationsChannelFeederMessage(IDictionary<string, object?> feederMessage) : this()
        {
            foreach (var item in feederMessage)
            {
                SetValue(item.Value, item.Key);
            }
        }

        /// <summary>
        /// Creates an independent copy of <paramref name="source"/>: every payload field (UserId,
        /// Subject, Body, etc.) plus the CastType/IsDeleted/CorrelationId/HashKey envelope values.
        /// All current fields are value types or immutable strings, so a per-field copy is already a
        /// full deep copy — the new instance shares no mutable state with <paramref name="source"/>.
        /// Changing a value on either instance afterward (e.g. retargeting UserId, or clearing the
        /// copy's HashKey via <see cref="ResetHashKey"/> before re-emitting to a specific recipient)
        /// never affects the other.
        /// </summary>
        internal NotificationsChannelFeederMessage(NotificationsChannelFeederMessage source) : this()
        {
            Guard.Against.Null(source);

            foreach (var item in (IReadOnlyDictionary<string, object?>)source)
                SetValue(item.Value, item.Key);

            CastType = source.CastType;
            IsDeleted = source.IsDeleted;
            CorrelationId = source.CorrelationId;
            Envelope.HashKey = source.Envelope.HashKey;
        }

        public string? UserId
        {
            get => GetValueOrNull<string>();
            set => SetValue(value);
        }

        public DateTime Date
        {
            get => GetValueOrDefault(DateTime.UtcNow);
            private set => SetValue(value);
        }

        public TimeSpan Time
        {
            get => GetValueOrDefault(DateTime.UtcNow.TimeOfDay);
            private set => SetValue(value);
        }

        public string Id
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
        }

        public string Origin
        {
            get => GetValueOrDefault(string.Empty);
            init => SetValue(value);
        }

        public NotificationType Type
        {
            get => GetValueOrDefault(NotificationType.Text);
            init => SetValue(value);
        }

        public NotificationPriority Priority
        {
            get => GetValueOrDefault(NotificationPriority.Normal);
            init => SetValue(value);
        }

        public string Icon
        {
            get => GetValueOrDefault(string.Empty);
            init => SetValue(value);
        }

        public string Subject
        {
            get => GetValueOrDefault(string.Empty);
            init => SetValue(value);
        }

        public string Body
        {
            get => GetValueOrDefault(string.Empty);
            init => SetValue(value);
        }

        /// <summary>
        /// An overflowed/preview form of <see cref="Body"/>, truncated to
        /// <see cref="EllipsisBodyThreshold"/> text elements with <see cref="EllipsisSuffix"/>
        /// appended when <see cref="Body"/> is longer. Derived automatically from <see cref="Body"/>
        /// unless explicitly set — an explicit value (including an empty string) is always honored
        /// as-is and never overwritten by the derivation.
        /// </summary>
        public string EllipsisBody
        {
            get => GetValueOrNull<string>() ?? DeriveEllipsisBody(Body);
            init => SetValue(value);
        }

        private static string DeriveEllipsisBody(string? body)
        {
            if (string.IsNullOrEmpty(body))
                return string.Empty;

            var textElements = new List<string>();
            var enumerator = StringInfo.GetTextElementEnumerator(body);
            while (enumerator.MoveNext())
                textElements.Add((string)enumerator.Current);

            return textElements.Count <= EllipsisBodyThreshold
                ? body
                : string.Concat(textElements.Take(EllipsisBodyThreshold)) + EllipsisSuffix;
        }

        public int Seen
        {
            get => GetValueOrDefault<int>(0);
            set => SetValue(value);
        }

        public string Metadata
        {
            get => GetValueOrDefault(string.Empty);
            init => SetValue(value);
        }

        internal NotificationsChannelFeederMessage ResetHashKey()
        {
            // The indexer (this["HashKey"] = ...) writes to the payload dictionary under a literal
            // "HashKey" key, which is a completely different storage location from the actual
            // envelope HashKey property read by EmitMessageAsync — that one is only reachable via
            // Envelope.HashKey, which is what this method actually needs to clear.
            Envelope.HashKey = null;
            return this;
        }
    }
}
