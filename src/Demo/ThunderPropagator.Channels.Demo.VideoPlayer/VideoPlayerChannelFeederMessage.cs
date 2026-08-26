using ThunderPropagator.BuildingBlocks.Application;

namespace ThunderPropagator.Channels.Demo.VideoPlayer
{
    /// <summary>
    /// This channel's JSON control/snapshot state — entirely separate from the binary
    /// <see cref="Media.Video.VideoFramePacket"/> transport (#214) carrying actual pixel data. Deliberately
    /// excludes any field naming the server-side media source (no URL, file path, or credential of any
    /// kind) — #215's own AC: "No client-facing state exposes the original source location."
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerChannelFeederMessage : FeederMessage
    {
        /// <summary>Maximum allowed length of <see cref="SessionId"/>.</summary>
        public const int SessionIdMaxLength = 128;

        /// <summary>Maximum allowed length of <see cref="VideoId"/>.</summary>
        public const int VideoIdMaxLength = 128;

        /// <summary>Maximum allowed length of <see cref="Host"/>.</summary>
        public const int HostMaxLength = 128;

        /// <summary>Maximum allowed length of <see cref="Title"/>.</summary>
        public const int TitleMaxLength = 300;

        /// <summary>Maximum number of distinct entries <see cref="Reactions"/> may hold.</summary>
        public const int ReactionsMaxCount = 50;

        /// <summary>Maximum allowed length of a single <see cref="VideoReactionCount.Reaction"/> name.</summary>
        public const int ReactionNameMaxLength = 32;

        /// <summary>Identifies which playback session this state belongs to — the only subscribing key.</summary>
        public string SessionId
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(ValidateNonEmpty(value, nameof(SessionId), SessionIdMaxLength));
        }

        /// <summary>
        /// The currently selected video's client-safe identifier (an approved-playlist entry id, per
        /// the parent epic's own <c>Video/Select</c> — never the underlying source path/URL itself).
        /// May be empty while <see cref="State"/> is <see cref="PlayState.Faulted"/> and no video was
        /// ever successfully resolved; see <see cref="ValidateForCurrentState"/>.
        /// </summary>
        public string VideoId
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(ValidateMaxLength(value, nameof(VideoId), VideoIdMaxLength));
        }

        /// <summary>The current video's human-readable title. Safe to display; never derived from a file path.</summary>
        public string Title
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(ValidateMaxLength(value, nameof(Title), TitleMaxLength));
        }

        /// <summary>This session's current lifecycle state.</summary>
        public PlayState State
        {
            get => GetValueOrDefault(PlayState.Loading);
            set => SetValue(value);
        }

        /// <summary>
        /// This session's current stream epoch — incremented by a seek or source change so clients (and
        /// in-flight <see cref="Media.Video.VideoFramePacket"/>s) from before that change can be recognized as
        /// stale. See <see cref="Media.Video.VideoFramePacket.Epoch"/>'s own remarks.
        /// </summary>
        public int Epoch
        {
            get => GetValueOrDefault(0);
            set => SetValue(ValidateNonNegative(value, nameof(Epoch)));
        }

        /// <summary>0-based number of the most recently published frame within <see cref="Epoch"/>.</summary>
        public long CurrentFrameNumber
        {
            get => GetValueOrDefault(0L);
            set => SetValue(ValidateNonNegative(value, nameof(CurrentFrameNumber)));
        }

        /// <summary>
        /// The playback position, in microseconds, that was current as of <see cref="SyncTime"/> —
        /// #215's own AC: "State serialization preserves microsecond positions." Represented as a plain
        /// integer (rather than <see cref="TimeSpan"/>) so it serializes as a wire number, matching this
        /// channel's numeric metadata descriptor for the field.
        /// </summary>
        public long MediaPosition
        {
            get => GetValueOrDefault(0L);
            set => SetValue(ValidateNonNegative(value, nameof(MediaPosition)));
        }

        /// <summary>
        /// The server media clock's own elapsed-microseconds reading at the moment
        /// <see cref="MediaPosition"/> was measured. Paired with <see cref="MediaPosition"/>, this is
        /// what lets a subscriber calculate the expected live position while <see cref="State"/> is
        /// <see cref="PlayState.Playing"/> — #215's own AC: "Snapshots contain enough data to calculate
        /// the expected live position." Not a wall-clock timestamp: the exact clock/synchronization
        /// scheme belongs to whichever future ticket implements pacing (#218) and client sync (#224);
        /// this field only carries the value that scheme measures.
        /// </summary>
        public long SyncTime
        {
            get => GetValueOrDefault(0L);
            set => SetValue(ValidateNonNegative(value, nameof(SyncTime)));
        }

        /// <summary>The current host's display name — the connection authorized for host-only commands. Always present once a session exists.</summary>
        public string Host
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(ValidateNonEmpty(value, nameof(Host), HostMaxLength));
        }

        /// <summary>Number of connections currently subscribed to this session.</summary>
        public int ViewerCount
        {
            get => GetValueOrDefault(0);
            set => SetValue(ValidateNonNegative(value, nameof(ViewerCount)));
        }

        /// <summary>The current video's total duration, in microseconds. Zero for a source of unknown or indeterminate (e.g. live) length.</summary>
        public long Duration
        {
            get => GetValueOrDefault(0L);
            set => SetValue(ValidateNonNegative(value, nameof(Duration)));
        }

        /// <summary>The underlying source's own frame rate, in frames per second (e.g. 23.976, 29.97, 60). Zero while unknown, such as before decoding has started.</summary>
        public double SourceFrameRate
        {
            get => GetValueOrDefault(0d);
            set => SetValue(ValidateNonNegative(value, nameof(SourceFrameRate)));
        }

        /// <summary>Current aggregate reaction counts. Empty until at least one reaction has been received.</summary>
        public IReadOnlyList<VideoReactionCount> Reactions
        {
            get => GetValueOrNull<IReadOnlyList<VideoReactionCount>>() ?? [];
            set => SetValue(ValidateReactions(value));
        }

        /// <summary>
        /// Checks this message's fields for internal consistency given its current <see cref="State"/>
        /// — #215's own AC: "Phase-specific validation rejects inconsistent state." Property setters
        /// only ever validate the single value being assigned (so they behave correctly regardless of
        /// object-initializer order); this method is the cross-field check a caller runs once every
        /// property has been set, mirroring <c>QuizChannelExtensions.AddQuizChannel</c>'s own
        /// post-configuration cross-property check.
        /// </summary>
        /// <exception cref="VideoPlayerChannelFeederMessageValidationException">
        /// <see cref="VideoId"/>/<see cref="Title"/> are empty while <see cref="State"/> is anything
        /// other than <see cref="PlayState.Faulted"/> (a fault is the only state that can occur before
        /// a video is ever resolved), or <see cref="SourceFrameRate"/> is not yet known while actively
        /// decoding (<see cref="PlayState.Playing"/>/<see cref="PlayState.Paused"/>/<see cref="PlayState.Buffering"/>).
        /// </exception>
        public void ValidateForCurrentState()
        {
            if (State != PlayState.Faulted)
            {
                if (string.IsNullOrWhiteSpace(VideoId))
                    throw new VideoPlayerChannelFeederMessageValidationException(nameof(VideoId), $"must not be empty while {nameof(State)} is {State}.");

                if (string.IsNullOrWhiteSpace(Title))
                    throw new VideoPlayerChannelFeederMessageValidationException(nameof(Title), $"must not be empty while {nameof(State)} is {State}.");
            }

            if ((State is PlayState.Playing or PlayState.Paused or PlayState.Buffering) && SourceFrameRate <= 0)
                throw new VideoPlayerChannelFeederMessageValidationException(nameof(SourceFrameRate), $"must be greater than zero while {nameof(State)} is {State}.");
        }

        private static string ValidateNonEmpty(string value, string propertyName, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new VideoPlayerChannelFeederMessageValidationException(propertyName, "must not be null, empty, or whitespace-only.");

            return ValidateMaxLength(value, propertyName, maxLength);
        }

        private static string ValidateMaxLength(string value, string propertyName, int maxLength)
        {
            if (value?.Length > maxLength)
                throw new VideoPlayerChannelFeederMessageValidationException(propertyName, $"must not exceed {maxLength} characters (was {value.Length}).");

            return value!;
        }

        private static int ValidateNonNegative(int value, string propertyName)
        {
            if (value < 0)
                throw new VideoPlayerChannelFeederMessageValidationException(propertyName, $"must not be negative (was {value}).");

            return value;
        }

        private static long ValidateNonNegative(long value, string propertyName)
        {
            if (value < 0)
                throw new VideoPlayerChannelFeederMessageValidationException(propertyName, $"must not be negative (was {value}).");

            return value;
        }

        private static double ValidateNonNegative(double value, string propertyName)
        {
            if (value < 0)
                throw new VideoPlayerChannelFeederMessageValidationException(propertyName, $"must not be negative (was {value}).");

            return value;
        }

        private static IReadOnlyList<VideoReactionCount> ValidateReactions(IReadOnlyList<VideoReactionCount> value)
        {
            if (value is null)
                return value!;

            if (value.Count > ReactionsMaxCount)
                throw new VideoPlayerChannelFeederMessageValidationException(nameof(Reactions), $"must not contain more than {ReactionsMaxCount} entries (had {value.Count}).");

            foreach (var reaction in value)
            {
                if (string.IsNullOrWhiteSpace(reaction.Reaction))
                    throw new VideoPlayerChannelFeederMessageValidationException(nameof(Reactions), "each entry's Reaction must not be null, empty, or whitespace-only.");

                if (reaction.Reaction.Length > ReactionNameMaxLength)
                    throw new VideoPlayerChannelFeederMessageValidationException(nameof(Reactions), $"each entry's Reaction must not exceed {ReactionNameMaxLength} characters (was {reaction.Reaction.Length}).");

                if (reaction.Count < 0)
                    throw new VideoPlayerChannelFeederMessageValidationException(nameof(Reactions), $"each entry's Count must not be negative (was {reaction.Count}).");
            }

            return value;
        }
    }
}
