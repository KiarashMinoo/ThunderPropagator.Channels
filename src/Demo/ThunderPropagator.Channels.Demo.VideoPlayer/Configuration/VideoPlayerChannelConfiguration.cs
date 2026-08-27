using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;
using ThunderPropagator.Channels.Demo.VideoPlayer.Playlist;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Configuration
{
    /// <summary>
    /// #234's own scope, "explicit settings for session/source selection, server output, encoding,
    /// lateness, audio, reactions, and retention behavior" — the single JSON-bindable root for every
    /// server-side VideoPlayer setting. Most of the individual values here already existed scattered
    /// across <see cref="Media.Session.VideoPlaybackSessionOptions"/>/<see cref="FfmpegVideoFrameSourceOptions"/>/
    /// <see cref="FfmpegAudioFrameSourceOptions"/> (#216-224) — this type aggregates them into one
    /// coherent, validated surface an operator actually configures. <see cref="Extensions.VideoPlayerChannelExtensions.AddVideoPlayerChannel"/>
    /// (#238) is what actually maps these values into those lower-level options types when constructing
    /// a real session, and constructs <see cref="Media.Session.VideoPlaybackSessionManager"/>/
    /// <see cref="IVideoPlaylist"/> from <see cref="PlaylistEntries"/>/<see cref="PlaylistPolicy"/> — this
    /// type itself still only defines, documents, and validates the settings themselves.
    /// </summary>
    public
#if !DEBUG
        sealed
#endif
        class VideoPlayerChannelConfiguration : AbstractChannelConfiguration
    {
        /// <summary>Smallest accepted value for <see cref="MaxWidth"/>/<see cref="MaxHeight"/>.</summary>
        public const int MinDimension = 1;

        /// <summary>
        /// Largest accepted value for <see cref="MaxWidth"/>/<see cref="MaxHeight"/> — 7680×4320 (8K) is
        /// already far beyond what this demo's own default (1280×720) or any realistic browser-viewed
        /// stream needs; the ceiling exists only to reject an obviously-mistyped value (e.g. an extra
        /// digit), not to model a real supported resolution.
        /// </summary>
        public const int MaxDimensionWidth = 7680;

        /// <summary>See <see cref="MaxDimensionWidth"/>'s own remarks — the same reasoning, sized for height instead.</summary>
        public const int MaxDimensionHeight = 4320;

        /// <summary>
        /// Largest accepted <see cref="AudioBitRate"/>, in bits per second — 512 kbps is already well
        /// above what either supported codec (Opus/AAC) needs for this demo's own audio quality (typical
        /// speech/music streaming bitrates run 32-192 kbps); the ceiling exists only to reject an
        /// obviously-mistyped value.
        /// </summary>
        public const int MaxAudioBitRate = 512_000;

        public VideoPlayerChannelConfiguration()
        {
            IsEnabled = true;
        }

        /// <summary>
        /// A stable, human-readable id for this deployment's one shared <see cref="Media.Session.VideoPlaybackSession"/>,
        /// in place of the runtime-generated <c>ChannelInfo.ChannelKey</c> GUID every <c>Video/*</c>
        /// pipeline currently derives its session id from. <see langword="null"/> (the default) leaves
        /// the runtime channel key as the session id, today's actual behavior — using this value instead
        /// is DI-construction wiring for #238 to do, same as every other not-yet-wired setting here; this
        /// property only defines and validates it. Max length matches
        /// <see cref="VideoPlayerChannelFeederMessage.SessionIdMaxLength"/>, the same bound the session id
        /// is already validated against once it reaches the wire.
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// A playlist id to auto-select so a demo shows something without requiring an explicit
        /// <c>Video/Select</c> call first. <see langword="null"/> (the default) means no auto-select —
        /// today's actual behavior. Actually calling <c>SelectAsync</c> with this value on
        /// startup/first-join is behavioral wiring no ticket in this family currently owns; this property
        /// only defines and validates it (including, when a playlist is available — see
        /// <see cref="Validate"/>'s own <c>playlist</c> parameter — confirming it resolves to a known,
        /// enabled entry). Max length matches <see cref="VideoPlayerChannelFeederMessage.VideoIdMaxLength"/>.
        /// </summary>
        public string? DefaultVideoId { get; set; }

        /// <summary>
        /// The server's approved video allow-list, by <see cref="VideoPlaylistEntry.VideoId"/> — #238's
        /// own scope, actually constructing the <see cref="IVideoPlaylist"/> <c>AddVideoPlayerChannel</c>
        /// registers (an <see cref="Playlist.InMemoryVideoPlaylist"/> built from exactly these entries and
        /// <see cref="PlaylistPolicy"/>). Every entry is validated against <see cref="PlaylistPolicy"/>
        /// (and checked for duplicate <see cref="VideoPlaylistEntry.VideoId"/>s) at that construction time
        /// — see <see cref="Playlist.InMemoryVideoPlaylist"/>'s own constructor remarks — so a
        /// misconfigured entry fails host startup the same way every other invalid setting here does,
        /// rather than surfacing later as a confusing runtime rejection. Default: empty — no video is
        /// selectable until an operator configures at least one entry, the correct default posture for a
        /// security-relevant allow-list (see <see cref="PlaylistPolicy"/>'s own remarks on deny-by-default).
        /// </summary>
        public IReadOnlyList<VideoPlaylistEntry> PlaylistEntries { get; set; } = [];

        /// <summary>
        /// The scheme/host/local-file-root rules every <see cref="PlaylistEntries"/> entry's own
        /// <see cref="VideoPlaylistEntry.Source"/> must satisfy — passed straight through to
        /// <see cref="Playlist.InMemoryVideoPlaylist"/>'s own constructor alongside
        /// <see cref="PlaylistEntries"/>. Default: <see cref="VideoPlaylistPolicy"/>'s own default (only
        /// <c>"file"</c> scheme allowed, no <see cref="VideoPlaylistPolicy.LocalFileRoot"/> configured, no
        /// remote hosts allowed) — approves nothing until explicitly configured, matching that type's own
        /// deny-by-default remarks.
        /// </summary>
        public VideoPlaylistPolicy PlaylistPolicy { get; set; } = new();

        /// <summary>
        /// Maximum output frame width, in pixels — mirrors <see cref="FfmpegVideoFrameSourceOptions.MaxWidth"/>.
        /// A source wider than this is scaled down (never up), preserving aspect ratio. Higher costs more
        /// encode CPU per frame and more bytes per frame (more outbound bandwidth per viewer). Default:
        /// 1280 — this demo's own 720p target.
        /// </summary>
        public int MaxWidth { get; set; } = 1280;

        /// <summary>Maximum output frame height, in pixels — mirrors <see cref="FfmpegVideoFrameSourceOptions.MaxHeight"/>. See <see cref="MaxWidth"/>'s own remarks. Default: 720.</summary>
        public int MaxHeight { get; set; } = 720;

        /// <summary>
        /// Which codec published <see cref="VideoFramePacket"/>s are encoded with — mirrors
        /// <see cref="Media.Session.VideoPlaybackSessionOptions.Encoding"/>. Default:
        /// <see cref="VideoFramePacketEncoding.Jpeg"/>.
        /// </summary>
        public VideoFramePacketEncoding Encoding { get; set; } = VideoFramePacketEncoding.Jpeg;

        /// <summary>
        /// Frame encode quality, 0 (smallest/lowest fidelity) to 100 (largest/highest fidelity) — mirrors
        /// <see cref="Media.Session.VideoPlaybackSessionOptions.Quality"/>, passed straight through to
        /// <see cref="VideoFrameEncoder.Encode"/>. Higher costs more encode CPU per frame and more bytes
        /// per frame. Default: 80.
        /// </summary>
        public int Quality { get; set; } = 80;

        /// <summary>
        /// Whether this deployment's session activates an audio pipeline at all — mirrors
        /// <see cref="Media.Session.VideoPlaybackSessionOptions.EnableAudio"/>. <see langword="false"/>
        /// removes audio's server runtime path entirely (no <see cref="IAudioFrameSource"/> ever
        /// opened, no <see cref="AudioFrameEncoder"/> ever constructed), regardless of every other
        /// audio setting below — those become inert, not invalid, when this is <see langword="false"/>
        /// (see <see cref="Validate"/>'s own remarks). Default: <see langword="true"/>.
        /// </summary>
        public bool EnableAudio { get; set; } = true;

        /// <summary>
        /// Forces one audio codec regardless of source — mirrors
        /// <see cref="Media.Session.VideoPlaybackSessionOptions.AudioEncoding"/>. <see langword="null"/>
        /// (the default) auto-detects from the selected source's own codec. Inert while
        /// <see cref="EnableAudio"/> is <see langword="false"/>.
        /// </summary>
        public AudioFramePacketEncoding? AudioEncoding { get; set; }

        /// <summary>
        /// Target audio bitrate, in bits per second — mirrors
        /// <see cref="Media.Session.VideoPlaybackSessionOptions.AudioBitRate"/>. Higher costs more
        /// outbound bandwidth per viewer with an active audio track. Inert while <see cref="EnableAudio"/>
        /// is <see langword="false"/>. Default: 64,000 (64 kbps).
        /// </summary>
        public int AudioBitRate { get; set; } = 64_000;

        /// <summary>
        /// Whether <c>Video/React</c> accepts any reaction at all for this deployment's session. A
        /// caller can already achieve the same effect by leaving <see cref="AllowedReactions"/> empty
        /// (nothing is ever a member of an empty set), but this explicit switch mirrors
        /// <see cref="EnableAudio"/>'s own naming/discoverability for an operator scanning the JSON.
        /// Removing reactions' server runtime path entirely (e.g. not registering the pipeline) is
        /// DI-wiring work for #238; this only controls whether every reaction is rejected. Default:
        /// <see langword="true"/>.
        /// </summary>
        public bool EnableReactions { get; set; } = true;

        /// <summary>
        /// The reaction strings accepted while <see cref="EnableReactions"/> is <see langword="true"/> —
        /// mirrors <see cref="Media.Session.VideoPlaybackSessionOptions.AllowedReactions"/>. Inert while
        /// <see cref="EnableReactions"/> is <see langword="false"/>. Default: a small built-in set
        /// (<c>like</c>, <c>love</c>, <c>laugh</c>, <c>wow</c>, <c>sad</c>, <c>clap</c>).
        /// </summary>
        public IReadOnlySet<string> AllowedReactions { get; set; } = new HashSet<string> { "like", "love", "laugh", "wow", "sad", "clap" };

        /// <summary>
        /// How long a recorded reaction stays visible in the aggregate snapshot and counts toward a
        /// viewer's own rate limit — mirrors <see cref="Media.Session.VideoPlaybackSessionOptions.ReactionWindow"/>.
        /// A shorter window with a high <see cref="MaxReactionsPerViewerPerWindow"/> costs more aggregator
        /// bookkeeping churn per viewer (more frequent pruning). Default: 10 seconds.
        /// </summary>
        public TimeSpan ReactionWindow { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The most reactions one viewer may record within any trailing <see cref="ReactionWindow"/> —
        /// mirrors <see cref="Media.Session.VideoPlaybackSessionOptions.MaxReactionsPerViewerPerWindow"/>.
        /// Default: 20.
        /// </summary>
        public int MaxReactionsPerViewerPerWindow { get; set; } = 20;

        /// <summary>
        /// Decoded-but-not-yet-published video frame buffer depth per generation — mirrors
        /// <see cref="Media.Session.VideoPlaybackSessionOptions.DecodeBufferCapacity"/>. Higher tolerates
        /// more decode/publish jitter before dropping frames, at the cost of more memory per active
        /// generation. Default: 8.
        /// </summary>
        public int DecodeBufferCapacity { get; set; } = 8;

        /// <summary>
        /// Per-viewer outbound video queue depth — mirrors
        /// <see cref="Media.Session.VideoPlaybackSessionOptions.SubscriberQueueCapacity"/>. Higher costs
        /// more memory per connected viewer. Default: 8.
        /// </summary>
        public int SubscriberQueueCapacity { get; set; } = 8;

        /// <summary>Decoded-but-not-yet-encoded audio frame buffer depth per generation — mirrors <see cref="Media.Session.VideoPlaybackSessionOptions.AudioDecodeBufferCapacity"/>. Inert while <see cref="EnableAudio"/> is <see langword="false"/>. Default: 16.</summary>
        public int AudioDecodeBufferCapacity { get; set; } = 16;

        /// <summary>Per-viewer outbound audio queue depth — mirrors <see cref="Media.Session.VideoPlaybackSessionOptions.AudioSubscriberQueueCapacity"/>. Inert while <see cref="EnableAudio"/> is <see langword="false"/>. Default: 16.</summary>
        public int AudioSubscriberQueueCapacity { get; set; } = 16;

        /// <summary>
        /// Playback speed multiplier passed to every <see cref="Media.FramePacer"/> a session creates —
        /// mirrors <see cref="Media.Session.VideoPlaybackSessionOptions.PlaybackRate"/>. Must be strictly
        /// positive; 1.0 is real-time. Default: 1.0.
        /// </summary>
        public double PlaybackRate { get; set; } = 1.0;

        /// <summary>
        /// How often the publish loop re-checks for a due frame while none is currently due, in
        /// milliseconds — mirrors <see cref="Media.Session.VideoPlaybackSessionOptions.PollInterval"/>.
        /// Lower costs more CPU from more frequent polling; higher costs more publish-latency jitter.
        /// Default: 5 ms.
        /// </summary>
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(5);

        /// <summary>
        /// How long a source open (<see cref="IVideoFrameSource.OpenAsync"/>/<see cref="IAudioFrameSource.OpenAsync"/>)
        /// may take before it should be treated as failed, or <see langword="null"/> for no timeout.
        /// Applied by wrapping the real source in <see cref="Media.Video.TimeoutVideoFrameSource"/>/
        /// <see cref="Media.Audio.TimeoutAudioFrameSource"/> (#238) — see those types' own remarks on why
        /// this depends on the wrapped source actually observing its own cancellation token (true for
        /// <see cref="FfmpegVideoFrameSource"/>/<see cref="FfmpegAudioFrameSource"/>, both already wired
        /// to FFmpeg's own AVIOInterruptCB mechanism). Default: 30 seconds — generous for a local file or
        /// a healthy remote source, without leaving a genuinely stuck open blocking a
        /// <c>Video/Select</c>/<c>Video/Seek</c> caller indefinitely.
        /// </summary>
        public TimeSpan? SourceOpenTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// How far publish may fall behind the shared timeline before a session should be considered
        /// <see cref="PlayState.Buffering"/> rather than merely jittering — this deployment's "server late
        /// tolerance." <see cref="Media.Session.VideoPlaybackSession"/> does not yet transition into
        /// <see cref="PlayState.Buffering"/> anywhere (verified: no call site sets it) — this property
        /// only defines, documents, and validates the threshold a future ticket would enforce; it has no
        /// runtime effect today. Default: 500 milliseconds.
        /// </summary>
        public TimeSpan MaxPublishLatenessBeforeBuffering { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// How long an idle (e.g. <see cref="PlayState.Ended"/>, no remaining subscribers) session should
        /// be retained by <see cref="Media.Session.VideoPlaybackSessionManager"/> before automatic
        /// removal, or <see langword="null"/> for no automatic cleanup (the actual current behavior —
        /// <c>VideoPlaybackSessionManager.RemoveSessionAsync</c> exists but nothing calls it based on
        /// idle time today). Actually wiring an automatic cleanup sweep is a future ticket's job; this
        /// property only defines and validates the retention window. Default: 5 minutes.
        /// </summary>
        public TimeSpan? IdleSessionRetention { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Called by <c>AddVideoPlayerChannel</c> immediately after the consumer's own
        /// <c>channelConfigurator</c> callback runs, so a misconfigured value fails host startup with a
        /// property-specific message rather than surfacing later as a confusing runtime failure — #234's
        /// own AC, "Invalid dimensions, quality, timeouts, encoding, or playlist references fail startup
        /// clearly." <paramref name="playlist"/> remains optional on this method's own signature (this
        /// type has no dependency on <see cref="IVideoPlaylist"/> otherwise, and stays independently
        /// testable without one), but <c>AddVideoPlayerChannel</c> (#238) always supplies the real
        /// <see cref="Playlist.InMemoryVideoPlaylist"/> it builds from <see cref="PlaylistEntries"/>/
        /// <see cref="PlaylistPolicy"/>, so <see cref="DefaultVideoId"/> is always cross-checked against
        /// it in practice — omitting <paramref name="playlist"/> only matters for a caller invoking this
        /// method directly (e.g. a test).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">A numeric/duration setting is outside its valid range, or an encoding is not a defined enum value.</exception>
        /// <exception cref="ArgumentException"><see cref="DefaultVideoId"/> is set but does not resolve to a known, enabled entry in <paramref name="playlist"/>.</exception>
        internal void Validate(IVideoPlaylist? playlist = null)
        {
            if (SessionId is not null)
            {
                if (string.IsNullOrWhiteSpace(SessionId))
                    throw new ArgumentOutOfRangeException(nameof(SessionId), SessionId, $"{nameof(SessionId)} must not be empty or whitespace-only when set.");

                if (SessionId.Length > VideoPlayerChannelFeederMessage.SessionIdMaxLength)
                    throw new ArgumentOutOfRangeException(nameof(SessionId), SessionId, $"{nameof(SessionId)} must not exceed {VideoPlayerChannelFeederMessage.SessionIdMaxLength} characters.");
            }

            if (DefaultVideoId is not null)
            {
                if (string.IsNullOrWhiteSpace(DefaultVideoId))
                    throw new ArgumentOutOfRangeException(nameof(DefaultVideoId), DefaultVideoId, $"{nameof(DefaultVideoId)} must not be empty or whitespace-only when set.");

                if (DefaultVideoId.Length > VideoPlayerChannelFeederMessage.VideoIdMaxLength)
                    throw new ArgumentOutOfRangeException(nameof(DefaultVideoId), DefaultVideoId, $"{nameof(DefaultVideoId)} must not exceed {VideoPlayerChannelFeederMessage.VideoIdMaxLength} characters.");
            }

            if (MaxWidth < MinDimension || MaxWidth > MaxDimensionWidth)
                throw new ArgumentOutOfRangeException(nameof(MaxWidth), MaxWidth, $"{nameof(MaxWidth)} must be between {MinDimension} and {MaxDimensionWidth}.");

            if (MaxHeight < MinDimension || MaxHeight > MaxDimensionHeight)
                throw new ArgumentOutOfRangeException(nameof(MaxHeight), MaxHeight, $"{nameof(MaxHeight)} must be between {MinDimension} and {MaxDimensionHeight}.");

            if (!Enum.IsDefined(Encoding))
                throw new ArgumentOutOfRangeException(nameof(Encoding), Encoding, $"{nameof(Encoding)} is not a supported {nameof(VideoFramePacketEncoding)} value.");

            if (Quality < VideoFrameEncoder.MinQuality || Quality > VideoFrameEncoder.MaxQuality)
                throw new ArgumentOutOfRangeException(nameof(Quality), Quality, $"{nameof(Quality)} must be between {VideoFrameEncoder.MinQuality} and {VideoFrameEncoder.MaxQuality}.");

            // Audio-only settings below are validated even while EnableAudio is false — they simply go
            // unused rather than becoming invalid, matching how VideoPlaybackSessionOptions.EnableAudio
            // itself already works (#224's own AC). A caller that has audio disabled today but flips it
            // on later without revisiting these should not be surprised by a value that was silently
            // never checked.
            if (AudioEncoding is { } audioEncoding && !Enum.IsDefined(audioEncoding))
                throw new ArgumentOutOfRangeException(nameof(AudioEncoding), AudioEncoding, $"{nameof(AudioEncoding)} is not a supported {nameof(AudioFramePacketEncoding)} value.");

            if (AudioBitRate < 1 || AudioBitRate > MaxAudioBitRate)
                throw new ArgumentOutOfRangeException(nameof(AudioBitRate), AudioBitRate, $"{nameof(AudioBitRate)} must be between 1 and {MaxAudioBitRate}.");

            if (AudioDecodeBufferCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(AudioDecodeBufferCapacity), AudioDecodeBufferCapacity, $"{nameof(AudioDecodeBufferCapacity)} must be at least 1.");

            if (AudioSubscriberQueueCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(AudioSubscriberQueueCapacity), AudioSubscriberQueueCapacity, $"{nameof(AudioSubscriberQueueCapacity)} must be at least 1.");

            // Same reasoning as the audio-only settings above: validated regardless of EnableReactions,
            // rather than becoming meaningful only once reactions are turned on.
            if (ReactionWindow <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ReactionWindow), ReactionWindow, $"{nameof(ReactionWindow)} must be greater than zero.");

            if (MaxReactionsPerViewerPerWindow < 1)
                throw new ArgumentOutOfRangeException(nameof(MaxReactionsPerViewerPerWindow), MaxReactionsPerViewerPerWindow, $"{nameof(MaxReactionsPerViewerPerWindow)} must be at least 1.");

            if (DecodeBufferCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(DecodeBufferCapacity), DecodeBufferCapacity, $"{nameof(DecodeBufferCapacity)} must be at least 1.");

            if (SubscriberQueueCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(SubscriberQueueCapacity), SubscriberQueueCapacity, $"{nameof(SubscriberQueueCapacity)} must be at least 1.");

            if (PlaybackRate <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(PlaybackRate), PlaybackRate, $"{nameof(PlaybackRate)} must be strictly positive.");

            if (PollInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(PollInterval), PollInterval, $"{nameof(PollInterval)} must be greater than zero.");

            if (SourceOpenTimeout is { } sourceOpenTimeout && sourceOpenTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(SourceOpenTimeout), SourceOpenTimeout, $"{nameof(SourceOpenTimeout)} must be greater than zero when set.");

            if (MaxPublishLatenessBeforeBuffering <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(MaxPublishLatenessBeforeBuffering), MaxPublishLatenessBeforeBuffering, $"{nameof(MaxPublishLatenessBeforeBuffering)} must be greater than zero.");

            if (IdleSessionRetention is { } idleSessionRetention && idleSessionRetention <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(IdleSessionRetention), IdleSessionRetention, $"{nameof(IdleSessionRetention)} must be greater than zero when set.");

            if (playlist is not null && DefaultVideoId is not null)
            {
                if (!playlist.TryGetEntry(DefaultVideoId, out var entry) || entry is null || !entry.IsEnabled)
                    throw new ArgumentException($"{nameof(DefaultVideoId)} '{DefaultVideoId}' does not resolve to a known, enabled playlist entry.", nameof(DefaultVideoId));
            }
        }
    }
}
