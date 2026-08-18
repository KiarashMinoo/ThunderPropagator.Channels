using System.Globalization;
using Ardalis.GuardClauses;
using ThunderPropagator.BuildingBlocks.Application;

namespace ThunderPropagator.Channels.Notifications
{
    /// <summary>
    /// The message emitted on and stored by the Notifications channel. Every field is
    /// dictionary-backed (see the base <see cref="FeederMessage"/>) — reading a field that was never
    /// set returns that field's documented default rather than throwing, and setting a field is what
    /// makes it visible to snapshot storage, historical queries, and routing. Recipient targeting is
    /// by <see cref="UserId"/> alone: leaving it unset routes the message as a broadcast, delivered
    /// to every current subscriber and to any subscriber who joins later without having missed it
    /// (see the channel's broadcast fan-out).
    /// </summary>
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

        /// <summary>
        /// Creates a new message and captures its <see cref="Date"/>/<see cref="Time"/> as of this
        /// instant (see the remarks on those properties). All other fields start unset at their
        /// documented defaults until explicitly assigned.
        /// </summary>
        public NotificationsChannelFeederMessage()
        {
            // Captured once here rather than left to each property's getter — reading Date and
            // Time repeatedly (or reading them apart in time) must observe the same instant this
            // message was constructed at, not the clock at the moment of each read.
            var now = DateTime.UtcNow;
            Date = now;
            Time = now.TimeOfDay;
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

        /// <summary>
        /// The intended recipient. Left null or whitespace, the message is a broadcast, delivered to
        /// every current subscriber rather than a single recipient — see the channel's fan-out
        /// behavior. This is the only field the channel uses for live subscription routing (#61).
        /// </summary>
        public string? UserId
        {
            get => GetValueOrNull<string>();
            set => SetValue(value);
        }

        /// <summary>
        /// UTC date this message was constructed. Captured once, from the same instant as
        /// <see cref="Time"/>, when the message is created — reading it repeatedly always returns the
        /// same value, and it never drifts from <see cref="Time"/> (see #64). Not settable directly;
        /// copying a message (or reconstructing one from a dictionary that already carries a Date
        /// entry) preserves the original value instead of capturing a new one.
        /// </summary>
        public DateTime Date
        {
            get => GetValueOrDefault(DateTime.UtcNow);
            private set => SetValue(value);
        }

        /// <summary>
        /// Time-of-day component matching <see cref="Date"/> — equal to <c>Date.TimeOfDay</c>,
        /// captured from the same instant. See <see cref="Date"/> for the stability and copying
        /// guarantees that apply equally here.
        /// </summary>
        public TimeSpan Time
        {
            get => GetValueOrDefault(DateTime.UtcNow.TimeOfDay);
            private set => SetValue(value);
        }

        /// <summary>
        /// Caller-assigned identifier for this notification. Empty string when unset. Combined with
        /// <see cref="UserId"/> to derive this message's snapshot storage identity, so two
        /// notifications sharing an Id for the same recipient are treated as updates to the same
        /// stored entry rather than two distinct notifications — assign a unique Id per logical
        /// notification if that's not the intended behavior.
        /// </summary>
        public string Id
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
        }

        /// <summary>Caller-defined source of this notification (e.g. the originating service or feature). Empty string when unset.</summary>
        public string Origin
        {
            get => GetValueOrDefault(string.Empty);
            init => SetValue(value);
        }

        /// <summary>Content format of <see cref="Body"/>. Defaults to <see cref="NotificationType.Text"/>.</summary>
        public NotificationType Type
        {
            get => GetValueOrDefault(NotificationType.Text);
            init => SetValue(value);
        }

        /// <summary>Relative importance of this notification. Defaults to <see cref="NotificationPriority.Normal"/>.</summary>
        public NotificationPriority Priority
        {
            get => GetValueOrDefault(NotificationPriority.Normal);
            init => SetValue(value);
        }

        /// <summary>Caller-defined icon identifier or reference (interpretation is up to the consuming client). Empty string when unset.</summary>
        public string Icon
        {
            get => GetValueOrDefault(string.Empty);
            init => SetValue(value);
        }

        /// <summary>Short heading for the notification. Empty string when unset.</summary>
        public string Subject
        {
            get => GetValueOrDefault(string.Empty);
            init => SetValue(value);
        }

        /// <summary>Full notification content. Empty string when unset. See <see cref="EllipsisBody"/> for its automatically derived preview form.</summary>
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

        /// <summary>
        /// Read/delivery state, encoded as a bitwise field per the channel's descriptor for this
        /// property rather than a plain boolean — this package defines no named bit constants, so
        /// interpretation of individual bits is left to the caller/consuming client. Defaults to 0
        /// (unseen).
        /// </summary>
        public int Seen
        {
            get => GetValueOrDefault<int>(0);
            set => SetValue(value);
        }

        /// <summary>Caller-defined JSON metadata for arbitrary extra data. Empty string when unset.</summary>
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
