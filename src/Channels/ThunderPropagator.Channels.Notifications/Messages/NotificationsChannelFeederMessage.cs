using System.Globalization;
using Ardalis.GuardClauses;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Notifications.Channel;
using ThunderPropagator.Channels.Notifications.Feeders;
using ThunderPropagator.Channels.Notifications.Messages;

namespace ThunderPropagator.Channels.Notifications.Messages
{
    /// <summary>
    /// The message emitted on and stored by the Notifications channel. Every field is
    /// dictionary-backed (see the base <see cref="FeederMessage"/>) — reading a field that was never
    /// set returns that field's documented default rather than throwing, and setting a field is what
    /// makes it visible to snapshot storage, historical queries, and routing. <see cref="Audience"/>
    /// (see #76) determines who a message reaches, rather than the presence or absence of
    /// <see cref="UserId"/>/<see cref="GroupId"/> alone:
    /// <see cref="NotificationAudience.Individual"/> delivers to that one recipient;
    /// <see cref="NotificationAudience.Group"/> delivers to recipients already known to belong to
    /// that group (see #74); <see cref="NotificationAudience.Broadcast"/> delivers to every current
    /// subscriber, and to any subscriber who joins later without having missed it (see the channel's
    /// fan-out). See <see cref="ValidateAudienceCombination"/> for exactly which of
    /// <see cref="UserId"/>/<see cref="GroupId"/> each value requires or forbids.
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

        /// <summary>Maximum allowed length of <see cref="Id"/>, enforced wherever Id is validated (see #68).</summary>
        public const int IdMaxLength = 256;

        /// <summary>Maximum allowed length of <see cref="Subject"/>, enforced wherever Subject is validated (see #68).</summary>
        public const int SubjectMaxLength = 200;

        /// <summary>Maximum number of distinct tags <see cref="Tags"/> may hold, enforced when it's assigned (see #74).</summary>
        public const int TagsMaxCount = 20;

        /// <summary>Maximum allowed length of a single tag within <see cref="Tags"/>, enforced when it's assigned (see #74).</summary>
        public const int TagMaxLength = 64;

        private const string EllipsisSuffix = "...";

        /// <summary>
        /// Creates a new message and captures its <see cref="Date"/>/<see cref="Time"/> as of this
        /// instant (see the remarks on those properties). All other fields start unset at their
        /// documented defaults until explicitly assigned.
        /// </summary>
        public NotificationsChannelFeederMessage() : this(TimeProvider.System)
        {
        }

        /// <summary>
        /// Test-only entry point (see #78) that captures <see cref="Date"/>/<see cref="Time"/> from
        /// <paramref name="timeProvider"/> instead of the real system clock, so a test can advance a
        /// fake clock between reads to prove they're stable without an actual wall-clock delay.
        /// </summary>
        internal NotificationsChannelFeederMessage(TimeProvider timeProvider)
        {
            // Captured once here rather than left to each property's getter — reading Date and
            // Time repeatedly (or reading them apart in time) must observe the same instant this
            // message was constructed at, not the clock at the moment of each read.
            var now = timeProvider.GetUtcNow().UtcDateTime;
            Date = now;
            Time = now.TimeOfDay;
        }

        /// <summary>
        /// Reconstructs a message from a raw field dictionary (e.g. a deserialized payload). Values
        /// are written directly to the payload rather than through the Id/Subject property
        /// accessors, so <see cref="ValidateRequiredFields"/> is run afterward to make sure this
        /// path can't produce a message with an invalid Id or Subject either.
        /// </summary>
        internal NotificationsChannelFeederMessage(IDictionary<string, object?> feederMessage) : this()
        {
            foreach (var item in feederMessage)
            {
                SetValue(item.Value, item.Key);
            }

            ValidateRequiredFields();
        }

        /// <summary>
        /// Creates an independent copy of <paramref name="source"/>: every payload field (UserId,
        /// Subject, Body, etc.) plus the CastType/IsDeleted/CorrelationId/HashKey envelope values.
        /// Every field except <see cref="Tags"/> is a value type or an immutable string, so a
        /// per-field copy is already a full deep copy for those — the new instance shares no mutable
        /// state with <paramref name="source"/> for them. <see cref="Tags"/> copies the same
        /// underlying read-only list reference rather than cloning it, which is safe because nothing
        /// in this package's public surface can mutate a list through its <see cref="IReadOnlyList{T}"/>-typed
        /// property. Changing a value on either instance afterward (e.g. retargeting UserId, or
        /// clearing the copy's HashKey via <see cref="ResetHashKey"/> before re-emitting to a
        /// specific recipient) never affects the other.
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

            ValidateRequiredFields();
        }

        /// <summary>
        /// The intended recipient — required when <see cref="Audience"/> is
        /// <see cref="NotificationAudience.Individual"/> (the default) and forbidden otherwise; see
        /// <see cref="ValidateAudienceCombination"/> (#76). This is the only field the channel uses
        /// for live subscription routing (#61).
        /// </summary>
        public string? UserId
        {
            get => GetValueOrNull<string>();
            set => SetValue(value);
        }

        /// <summary>
        /// The intended audience group — required when <see cref="Audience"/> is
        /// <see cref="NotificationAudience.Group"/>, forbidden when it's
        /// <see cref="NotificationAudience.Broadcast"/>, and optional (usable purely for
        /// categorization/filtering, with no routing effect) when it's
        /// <see cref="NotificationAudience.Individual"/> — see
        /// <see cref="ValidateAudienceCombination"/> (#76). A message with the Group audience is
        /// routed only to recipients already known to be members of this group — see the channel's group
        /// fan-out (#74) — rather than to every current subscriber the way
        /// <see cref="NotificationAudience.Broadcast"/> is. A recipient becomes a known member the
        /// same way a broadcast recipient does: by having previously received a targeted,
        /// <c>CastType.Broadcast</c>-tagged message carrying this same GroupId. Comparison is ordinal
        /// (case-sensitive) and exact — unlike <see cref="Tags"/>, GroupId has no normalization or
        /// deduplication of its own since it's a single scalar value, not a collection.
        /// </summary>
        public string? GroupId
        {
            get => GetValueOrNull<string>();
            init => SetValue(value);
        }

        /// <summary>
        /// Who this message is routed to (see #76). Defaults to <see cref="NotificationAudience.Individual"/>
        /// — the safe default, since it's the value that requires a specific
        /// <see cref="UserId"/> rather than letting an unset one reach every subscriber by accident.
        /// The channel routes strictly by this value rather than by inferring intent from which of
        /// <see cref="UserId"/>/<see cref="GroupId"/> happens to be set — see
        /// <see cref="ValidateAudienceCombination"/> for the combinations each value requires or
        /// forbids, checked by the channel immediately before routing (not on every property set,
        /// since a message built via the copy constructor can legitimately carry a combination that
        /// would be invalid for a caller-authored message — e.g. a stored broadcast recipient copy
        /// keeps <see cref="NotificationAudience.Broadcast"/> alongside the specific
        /// <see cref="UserId"/> it was delivered to).
        /// </summary>
        public NotificationAudience Audience
        {
            get => GetValueOrDefault(NotificationAudience.Individual);
            init => SetValue(value);
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
        /// Caller-assigned identifier for this notification. Empty string when unset — but a set
        /// value can never be null, empty, or whitespace-only, nor exceed <see cref="IdMaxLength"/>
        /// characters: assigning such a value throws
        /// <see cref="NotificationsChannelFeederMessageValidationException"/> immediately (see #68).
        /// Leaving Id unset entirely doesn't throw here, since object-initializer construction sets
        /// properties after the constructor runs — but the channel rejects an empty Id at the
        /// emission boundary (<c>NotificationsChannel.EmitMessage</c>/<c>EmitMessageAsync</c>), so a
        /// message can't actually be sent without one. Combined with <see cref="UserId"/> to
        /// derive this message's snapshot storage identity, so two notifications sharing an Id for
        /// the same recipient are treated as updates to the same stored entry rather than two
        /// distinct notifications — assign a unique Id per logical notification if that's not the
        /// intended behavior.
        /// </summary>
        public string Id
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(ValidateExplicitValue(value, nameof(Id), IdMaxLength));
        }

        /// <summary>Caller-defined source of this notification (e.g. the originating service or feature). Empty string when unset.</summary>
        public string Origin
        {
            get => GetValueOrDefault(string.Empty);
            init => SetValue(value);
        }

        /// <summary>
        /// Content format of <see cref="Body"/>. Defaults to <see cref="NotificationContentType.Text"/>.
        /// Independent of <see cref="Category"/> — see #69.
        /// </summary>
        public NotificationContentType Type
        {
            get => GetValueOrDefault(NotificationContentType.Text);
            init => SetValue(value);
        }

        /// <summary>
        /// Semantic category of this notification. Defaults to <see cref="NotificationCategory.Info"/>.
        /// Independent of <see cref="Type"/> — see #69.
        /// </summary>
        public NotificationCategory Category
        {
            get => GetValueOrDefault(NotificationCategory.Info);
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

        /// <summary>
        /// Short heading for the notification. Empty string when unset — but a set value can never
        /// be null, empty, or whitespace-only, nor exceed <see cref="SubjectMaxLength"/> characters:
        /// assigning such a value throws <see cref="NotificationsChannelFeederMessageValidationException"/>
        /// immediately (see #68). As with <see cref="Id"/>, leaving Subject unset entirely doesn't
        /// throw here, but the channel rejects an empty Subject at the emission boundary.
        /// </summary>
        public string Subject
        {
            get => GetValueOrDefault(string.Empty);
            init => SetValue(ValidateExplicitValue(value, nameof(Subject), SubjectMaxLength));
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
        /// Delivery/read lifecycle state (see <see cref="NotificationDeliveryState"/>). Defaults to
        /// <see cref="NotificationDeliveryState.None"/>. The setter rejects a value with any bit set
        /// outside the defined flags, throwing
        /// <see cref="NotificationsChannelFeederMessageValidationException"/> — see #70. A raw
        /// integer previously stored under this same field name (e.g. by an earlier version of this
        /// package) is still read correctly: a <see cref="System.FlagsAttribute"/> enum's underlying
        /// representation is an <see cref="int"/>, so a legacy value maps onto these flags bit-for-bit
        /// with no stored-data migration needed.
        /// </summary>
        public NotificationDeliveryState Seen
        {
            get => GetValueOrNull<object>() switch
            {
                int legacyValue => (NotificationDeliveryState)legacyValue,
                _ => GetValueOrDefault(NotificationDeliveryState.None)
            };
            set => SetValue(ValidateDeliveryState(value));
        }

        /// <summary>
        /// Rejects a <see cref="NotificationDeliveryState"/> with any bit set outside the defined
        /// flags. Internal rather than private so <c>NotificationsChannel.AcknowledgeAsync</c> (see
        /// #77) can validate a caller-supplied state up front, before doing any lookup work, using
        /// the exact same rule the <see cref="Seen"/> setter itself enforces.
        /// </summary>
        internal static NotificationDeliveryState ValidateDeliveryState(NotificationDeliveryState value)
        {
            const NotificationDeliveryState allDefinedFlags = NotificationDeliveryState.Delivered
                | NotificationDeliveryState.Seen
                | NotificationDeliveryState.Read
                | NotificationDeliveryState.Dismissed;

            if ((value & ~allDefinedFlags) != 0)
                throw new NotificationsChannelFeederMessageValidationException(nameof(Seen), $"must only combine defined NotificationDeliveryState flags (was {value}).");

            return value;
        }

        /// <summary>
        /// Optional UTC instant after which this notification is treated as expired (see #73):
        /// excluded from snapshot replay, from historical queries, and from missed-broadcast
        /// catch-up once expired, and rejected outright if it's already expired at the moment
        /// <c>EmitMessage</c>/<c>EmitMessageAsync</c> is called (skipped with a logged notice, not
        /// thrown — an expired message is an expected, benign outcome rather than a caller error).
        /// The boundary is inclusive: this message is treated as expired the instant the clock
        /// reaches <see cref="ExpiresAt"/>, not strictly after it.
        /// </summary>
        /// <remarks>
        /// Null (the default) means this message never expires on its own. This is a per-message
        /// value only — the channel holds no reference to a feeder's own
        /// <see cref="NotificationsFeederConfiguration.TimeToLive"/> and can't derive an expiration
        /// from it automatically. A feeder implementation that wants that default TTL enforced by
        /// the channel must compute and assign this property itself (e.g. <c>Date + TimeToLive</c>)
        /// before emitting; leaving it unset means <see cref="NotificationsFeederConfiguration.TimeToLive"/>
        /// remains purely advisory for this message, exactly as it was before this property existed.
        /// </remarks>
        public DateTime? ExpiresAt
        {
            // GetValueOrNull<T>'s generic implementation resolves an unset key's "default" branch
            // as default(T) rather than default(T?), so calling it directly with a value type (e.g.
            // GetValueOrNull<DateTime>()) returns DateTime.MinValue instead of null when ExpiresAt
            // was never set. Reading through GetValueOrNull<object>() first and casting sidesteps
            // that — object is a reference type, so its own default is already null — matching the
            // same defensive pattern Seen already uses below.
            get => GetValueOrNull<object>() as DateTime?;
            init => SetValue(value);
        }

        /// <summary>Caller-defined JSON metadata for arbitrary extra data. Empty string when unset.</summary>
        public string Metadata
        {
            get => GetValueOrDefault(string.Empty);
            init => SetValue(value);
        }

        /// <summary>
        /// Free-form categorization/filtering labels (see #74) — see
        /// <see cref="NotificationsChannel{T}.SearchHistoricalNotificationsAsync"/> for querying by
        /// tag. Never null: an empty collection when never assigned, regardless of whether this
        /// instance came from the parameterless constructor, the dictionary constructor, or the copy
        /// constructor. Comparison and deduplication are case-insensitive (ordinal), but the
        /// originally-assigned casing of the first occurrence of each distinct tag is preserved in
        /// storage and on read — assigning <c>["Urgent", "urgent"]</c> stores just <c>["Urgent"]</c>.
        /// Insertion order (of first occurrences) is preserved otherwise. Assigning a collection
        /// throws <see cref="NotificationsChannelFeederMessageValidationException"/> immediately if
        /// any tag is null, empty, or whitespace-only, if any tag exceeds <see cref="TagMaxLength"/>
        /// characters, or if the deduplicated tag count exceeds <see cref="TagsMaxCount"/>. There's no
        /// restriction on which characters a tag may contain beyond those length rules — this
        /// package doesn't enforce a fixed vocabulary of allowed tag values.
        /// </summary>
        public IReadOnlyList<string> Tags
        {
            get => GetValueOrNull<IReadOnlyList<string>>() ?? [];
            init => SetValue(NormalizeTags(value));
        }

        private static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string>? tags)
        {
            if (tags is null || tags.Count == 0)
                return [];

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var normalized = new List<string>(tags.Count);

            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    throw new NotificationsChannelFeederMessageValidationException(nameof(Tags), "must not contain a null, empty, or whitespace-only tag.");

                if (tag.Length > TagMaxLength)
                    throw new NotificationsChannelFeederMessageValidationException(nameof(Tags), $"must not contain a tag longer than {TagMaxLength} characters (was {tag.Length}).");

                if (seen.Add(tag))
                    normalized.Add(tag);
            }

            if (normalized.Count > TagsMaxCount)
                throw new NotificationsChannelFeederMessageValidationException(nameof(Tags), $"must not contain more than {TagsMaxCount} distinct tags (had {normalized.Count}).");

            return normalized;
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

        /// <summary>
        /// Validates a value being explicitly assigned to Id or Subject: rejects null, empty, or
        /// whitespace-only, and rejects a value over <paramref name="maxLength"/> characters.
        /// Doesn't run for a property that's simply never touched — that's caught separately by
        /// <see cref="ValidateRequiredFields"/> at the constructor/emission boundary, since an
        /// object initializer only assigns properties after the constructor has already returned.
        /// </summary>
        private static string ValidateExplicitValue(string? value, string propertyName, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new NotificationsChannelFeederMessageValidationException(propertyName, "must not be null, empty, or whitespace-only.");

            if (value.Length > maxLength)
                throw new NotificationsChannelFeederMessageValidationException(propertyName, $"must not exceed {maxLength} characters (was {value.Length}).");

            return value;
        }

        /// <summary>
        /// Verifies <see cref="Id"/> and <see cref="Subject"/> are both set to a valid value (see
        /// <see cref="ValidateExplicitValue"/> for what "valid" means) — the check that actually
        /// catches a property that was never touched at all, which the property setters alone
        /// cannot catch since an unset property never invokes them. Called at the end of the
        /// dictionary and copy constructors (the two paths that write the payload directly rather
        /// than through the property accessors) and by the channel immediately before emitting a
        /// message, which is the earliest point a message built via the public parameterless
        /// constructor and object initializer can reliably be checked.
        /// </summary>
        internal void ValidateRequiredFields()
        {
            ValidateExplicitValue(Id, nameof(Id), IdMaxLength);
            ValidateExplicitValue(Subject, nameof(Subject), SubjectMaxLength);
        }

        /// <summary>
        /// Verifies <see cref="Audience"/>'s required and forbidden combination with
        /// <see cref="UserId"/> and <see cref="GroupId"/> (see #76): <see cref="NotificationAudience.Individual"/>
        /// requires UserId; <see cref="NotificationAudience.Group"/> requires GroupId and forbids
        /// UserId; <see cref="NotificationAudience.Broadcast"/> forbids both. Individual doesn't
        /// forbid GroupId — a message can be addressed to one specific recipient while still
        /// carrying a GroupId purely for categorization/filtering (see #74's
        /// <c>SearchHistoricalNotificationsAsync</c> groupId filter), since GroupId only affects
        /// routing for the Group audience. Deliberately <b>not</b> called from either constructor the
        /// way <see cref="ValidateRequiredFields"/> is — the channel's own fan-out constructs a
        /// per-recipient copy of a Group/Broadcast-audience message via the copy constructor and
        /// then sets that copy's <see cref="UserId"/>, a combination this method would otherwise
        /// reject despite being entirely legitimate (it's how a delivered copy records who received
        /// it while still recording which audience it was originally sent to). Called by the channel
        /// only against the original, caller-authored message immediately before routing it.
        /// </summary>
        internal void ValidateAudienceCombination()
        {
            switch (Audience)
            {
                case NotificationAudience.Individual:
                    if (string.IsNullOrWhiteSpace(UserId))
                        throw new NotificationsChannelFeederMessageValidationException(nameof(Audience), $"{NotificationAudience.Individual} requires {nameof(UserId)} to be set.");
                    break;

                case NotificationAudience.Group:
                    if (string.IsNullOrWhiteSpace(GroupId))
                        throw new NotificationsChannelFeederMessageValidationException(nameof(Audience), $"{NotificationAudience.Group} requires {nameof(GroupId)} to be set.");
                    if (!string.IsNullOrWhiteSpace(UserId))
                        throw new NotificationsChannelFeederMessageValidationException(nameof(Audience), $"{NotificationAudience.Group} must not set {nameof(UserId)}.");
                    break;

                case NotificationAudience.Broadcast:
                    if (!string.IsNullOrWhiteSpace(UserId))
                        throw new NotificationsChannelFeederMessageValidationException(nameof(Audience), $"{NotificationAudience.Broadcast} must not set {nameof(UserId)}.");
                    if (!string.IsNullOrWhiteSpace(GroupId))
                        throw new NotificationsChannelFeederMessageValidationException(nameof(Audience), $"{NotificationAudience.Broadcast} must not set {nameof(GroupId)}.");
                    break;
            }
        }
    }
}
