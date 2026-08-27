using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Diagnostics;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session
{
    /// <summary>
    /// Owns exactly one active <see cref="IVideoFrameSource"/>, <see cref="DecodedFrameBuffer"/>,
    /// <see cref="FramePacer"/>, decode loop, and publish loop for one playback session — #220's own
    /// scope for a single session (see <see cref="VideoPlaybackSessionManager"/> for the collection of
    /// these keyed by <see cref="SessionId"/>). Every viewer subscribed via <see cref="Subscribe"/> reads
    /// from the same decode loop and shared timeline; a viewer's own <see cref="SubscriberFrameQueue{T}"/>
    /// is independent of every other viewer's and of the decode loop itself, so joining or leaving never
    /// restarts or duplicates decoding — #220's own AC.
    /// </summary>
    /// <remarks>
    /// <b>Concurrent command contract</b> — #232's own scope, "define idempotency/conflict responses":
    /// every mutating lifecycle method (<see cref="SelectAsync"/>/<see cref="SeekAsync"/> via
    /// <c>SwitchGenerationAsync</c>, <see cref="PauseAsync"/>, <see cref="ResumeAsync"/>,
    /// <see cref="EndAsync"/>, <see cref="DisposeAsync"/>) is serialized against every other one by
    /// <c>_lifecycleLock</c>, so at most one is ever mutating playback state at a time — this session's own
    /// per-session async command coordinator, even though it isn't packaged as a separately-named type.
    /// Non-mutating reads (<see cref="State"/>, <see cref="Epoch"/>, <see cref="PeekSnapshot"/>,
    /// <see cref="Join"/>, <see cref="IsHost"/>, etc.) never take that lock, only the much cheaper
    /// <c>_publishGate</c> (or nothing at all), so they always proceed even while a mutation is in flight.
    /// Concurrent <see cref="SelectAsync"/>/<see cref="SeekAsync"/> calls apply in the order each one's own
    /// fast, lock-held "claim an epoch" step actually runs — not the order their own (possibly slow)
    /// source-open completes — so the call whose claim happens <i>last</i> always wins, and every
    /// earlier-still-in-flight attempt is silently abandoned (its own opened source disposed, session state
    /// left untouched) rather than erroring. <see cref="PauseAsync"/>/<see cref="ResumeAsync"/> are
    /// idempotent no-ops when called while the timeline is already paused/already running, respectively
    /// (see <see cref="FramePacer.Pause"/>/<see cref="FramePacer.Resume"/>'s own remarks) — calling either
    /// repeatedly is always safe. <see cref="EndAsync"/> tears down whatever generation was active (a no-op
    /// if none was) and always leaves the session in <see cref="PlayState.Ended"/>, so repeated calls are
    /// likewise safe. None of these session-level semantics require a distinct "conflict" response type —
    /// the pipeline layer (<c>Video/Play</c>/<c>Pause</c>/<c>Seek</c>/<c>Select</c>) already interprets
    /// session state at the wire level to decide idempotent-success vs. rejection for its own callers; this
    /// paragraph documents what actually happens underneath those decisions.
    /// <para/>
    /// <b>Generations:</b> each call to <see cref="SelectAsync"/>/<see cref="SeekAsync"/> replaces the
    /// previously active source/buffer/pacer/loop-pair (a "generation") with a new one and increments
    /// <see cref="Epoch"/>. A generation stops in exactly one of three ways — an explicit lifecycle call
    /// (<see cref="SelectAsync"/> superseding it, <see cref="EndAsync"/>, or <see cref="DisposeAsync"/>),
    /// a decode/publish fault, or the source naturally running out of frames — and in every case its
    /// media resources (source, buffer) are disposed exactly once, only after both its loops have
    /// actually stopped — #220's own AC, "Dispose media resources only after session work has stopped."
    /// A fault or natural end detected on a generation that has since been superseded by a newer one is
    /// recognized as stale and never overwrites the newer generation's state.
    /// <para/>
    /// <b>Simplification:</b> <see cref="SeekAsync"/> re-opens a fresh source at the new position rather
    /// than re-seeking the previously open one (which <see cref="IVideoFrameSource.ReadFramesAsync"/>'s
    /// own contract also permits) — this keeps exactly one resource-lifetime model for every generation
    /// this type ever creates, which is what this ticket's concurrency/dispose-ordering ACs are actually
    /// about; reusing an already-open source across a seek is a possible future optimization, not a
    /// correctness requirement here.
    /// <para/>
    /// <b>Late join:</b> <see cref="Join"/> is how a viewer joining mid-playback starts at the current
    /// live position instead of frame 0, without creating a second decoder/timeline — #223's own scope.
    /// It and <see cref="PublishFrame"/> are the only two places that touch <c>_lastPublishedFrame</c>,
    /// always under the same lock, which is what makes a join's own "subscribe, then unicast whatever
    /// was last published" atomic relative to a concurrently-running publish's own "record what was just
    /// published, then deliver it to whoever is currently subscribed": either the join's critical section
    /// runs first (it unicasts the prior frame, then the in-flight publish's own delivery reaches the
    /// newly-subscribed viewer normally — correct order, no duplicate), or the publish's runs first (its
    /// delivery pass does not yet include the joining viewer, and the join's own unicast — reading
    /// <c>_lastPublishedFrame</c> after that publish updated it — delivers exactly that one frame instead
    /// — also correct, no duplicate, no gap). Without this, "subscribe" and "read the last frame to
    /// unicast" as two separate, unlocked steps could race a concurrent publish either way and either
    /// duplicate a frame or momentarily rewind — #223's own AC, "Duplicate/racing frames cannot cause
    /// visible rewind." <c>_lastPublishedFrame</c> is cleared whenever a new generation starts, so a join
    /// racing a <see cref="SelectAsync"/>/<see cref="SeekAsync"/> call never hands out a frame from a
    /// superseded epoch — #223's own AC, "Snapshot and bootstrap belong to the same epoch."
    /// <para/>
    /// <b>Audio:</b> #224's own scope. Every viewer's audio queue is independent of its video one, but
    /// both are populated from the very same generation's <see cref="FramePacer"/>, so
    /// <see cref="AudioFramePacket.DisplayTime"/> and <see cref="VideoFramePacket.DisplayTime"/> are
    /// always on one synchronized clock — #224's own AC, "Audio and video packets share one session
    /// epoch and synchronized timestamps." Audio activates only when <c>audioSourceFactory</c> is
    /// supplied to the constructor <i>and</i> <see cref="VideoPlaybackSessionOptions.EnableAudio"/> is
    /// <see langword="true"/> — a session missing either runs video-only, with no
    /// <see cref="IAudioFrameSource"/> ever opened and no <see cref="AudioFrameEncoder"/> ever
    /// constructed — #224's own AC, "Muted/video-only sessions run without audio resources." A source
    /// that turns out to have no audio track (or whose audio otherwise fails to open) is treated the
    /// same way: this session keeps playing video-only rather than faulting outright, since a broken or
    /// absent audio track should never be able to stop video that is otherwise healthy. The same
    /// tolerance applies to a runtime audio decode/encode failure after a generation has already
    /// started — it silently stops that generation's own audio (video is unaffected) rather than
    /// faulting the whole session, unlike a video-side failure, which does fault the session (video is
    /// this type's own primary contract; audio is always a value-add on top of it, consistent with
    /// letting a session run muted/video-only in the first place). Audio decode/publish share the exact
    /// same generation/epoch/cancellation machinery as video (see this type's own remarks on
    /// generations), so a seek, source change, epoch change, or session removal resets/disposes audio
    /// work exactly as coherently as it already does for video — #224's own AC.
    /// </remarks>
    public sealed class VideoPlaybackSession : IAsyncDisposable
    {
        private readonly Func<IVideoFrameSource> _sourceFactory;
        private readonly Func<IAudioFrameSource>? _audioSourceFactory;
        private readonly Func<int, int, AudioFramePacketEncoding, IAudioEncoder> _audioEncoderFactory;
        private readonly IMonotonicClock _clock;
        private readonly Func<DecodedVideoFrame, ReadOnlyMemory<byte>> _encodeFrame;
        private readonly VideoPlaybackSessionOptions _options;
        private readonly Action<FrameDropReason>? _onFrameDropped;
        private readonly VideoPlaybackTelemetry? _telemetry;
        private readonly ILogger? _logger;
        private readonly CancellationToken _hostShutdownToken;
        private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
        private readonly ConcurrentDictionary<string, SubscriberFrameQueue<VideoFramePacket>> _subscribers = new();
        private readonly ConcurrentDictionary<string, SubscriberFrameQueue<AudioFramePacket>> _audioSubscribers = new();

        // Tracks *when* each currently-subscribed viewer joined (an arbitrary monotonic sequence
        // number, not a wall-clock time) — #231's own scope, purely to pick a deterministic "next host"
        // on reassignment (earliest-joined-still-subscribed wins). Deliberately separate from
        // _subscribers/_audioSubscribers, which only need "is this viewer here," never "when."
        private readonly ConcurrentDictionary<string, long> _subscriberJoinOrder = new();
        private long _nextJoinSequence;

        // Guards every host-state mutation (first-subscriber assignment, disconnect-driven
        // reassignment) — #231's own scope. Deliberately a lock separate from _publishGate/
        // _lifecycleLock: host bookkeeping has nothing to do with the decode/publish hot path or the
        // generation-switch lifecycle, and sharing either of those locks here would be an unforced
        // cross-concern coupling for no benefit.
#if NET9_0_OR_GREATER
        private readonly Lock _hostLock = new();
#else
        private readonly object _hostLock = new();
#endif

        // Guards _lastPublishedFrame together with "which viewers are currently subscribed" as one
        // atomic unit, shared by PublishFrame and Join — #223's own scope. A plain sync lock (never held
        // across an await) rather than _lifecycleLock: PublishFrame runs on every decoded frame, a much
        // hotter path than any lifecycle call, and reusing the async lifecycle semaphore here would add
        // needless overhead and an unrelated cross-lock-ordering concern for no benefit. See this type's
        // own remarks on how Join uses it to prevent duplicate/rewound delivery.
#if NET9_0_OR_GREATER
        private readonly Lock _publishGate = new();
#else
        private readonly object _publishGate = new();
#endif
        private VideoFramePacket? _lastPublishedFrame;
        private AudioFramePacket? _lastPublishedAudioPacket;

        // -1 = no audio active for the current generation; otherwise (int)AudioFramePacketEncoding.
        // A nullable enum can't itself be marked volatile, so this is the lock-free-readable encoding
        // behind the public AudioEncoding property — see this type's own remarks on audio.
        private volatile int _audioEncodingRaw = -1;

        private volatile PlayState _state = PlayState.Loading;
        private volatile Exception? _fault;
        private Generation? _current;
        private VideoSource? _currentSource;
        private VideoStreamInfo? _currentStreamInfo;
        private string? _hostConnectionId;
        private int _epoch;
        private long _nextFrameNumber;
        private long _nextAudioPacketNumber;
        private bool _disposed;

        public VideoPlaybackSession(
            string sessionId,
            Func<IVideoFrameSource> sourceFactory,
            IMonotonicClock clock,
            VideoPlaybackSessionOptions? options = null,
            Func<DecodedVideoFrame, ReadOnlyMemory<byte>>? encodeFrame = null,
            Action<FrameDropReason>? onFrameDropped = null,
            CancellationToken hostShutdownToken = default,
            Func<IAudioFrameSource>? audioSourceFactory = null,
            Func<int, int, AudioFramePacketEncoding, IAudioEncoder>? audioEncoderFactory = null,
            VideoPlaybackTelemetry? telemetry = null,
            ILogger? logger = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ArgumentNullException.ThrowIfNull(sourceFactory);
            ArgumentNullException.ThrowIfNull(clock);

            _options = options ?? new VideoPlaybackSessionOptions();
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_options.DecodeBufferCapacity, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_options.SubscriberQueueCapacity, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_options.PlaybackRate, 0.0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_options.AudioDecodeBufferCapacity, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_options.AudioSubscriberQueueCapacity, 0);

            SessionId = sessionId;
            _sourceFactory = sourceFactory;
            _audioSourceFactory = audioSourceFactory;
            _audioEncoderFactory = audioEncoderFactory ?? ((sampleRate, channels, encoding) => new AudioFrameEncoder(sampleRate, channels, encoding, _options.AudioBitRate));
            _clock = clock;
            _encodeFrame = encodeFrame ?? (frame => VideoFrameEncoder.Encode(frame, _options.Encoding, _options.Quality));
            _onFrameDropped = onFrameDropped;
            _telemetry = telemetry;
            _logger = logger;
            _hostShutdownToken = hostShutdownToken;

            Reactions = new ReactionAggregator(clock, _options.AllowedReactions, _options.ReactionWindow, _options.MaxReactionsPerViewerPerWindow);
        }

        /// <summary>Identifies this session — the same value every <see cref="VideoFramePacket.SessionId"/> it publishes carries.</summary>
        public string SessionId { get; }

        /// <summary>This session's current lifecycle state. Safe to read from any thread without external locking.</summary>
        public PlayState State => _state;

        /// <summary>The exception a fault transitioned this session with, or <see langword="null"/> if it never faulted.</summary>
        public Exception? Fault => _fault;

        /// <summary>The current stream epoch — incremented by every <see cref="SelectAsync"/>/<see cref="SeekAsync"/> call that actually starts a new generation.</summary>
        public int Epoch => Volatile.Read(ref _epoch);

        /// <summary>The most recently selected source, or <see langword="null"/> if <see cref="SelectAsync"/> has never been called for this session. Distinct from <see cref="State"/>: a freshly constructed session is also <see cref="PlayState.Loading"/> before any source is ever selected, so this is the only reliable "has anything been selected yet" signal.</summary>
        public VideoSource? CurrentSource => _currentSource;

        /// <summary>
        /// The most recently opened source's own stream duration, or <see langword="null"/> if
        /// <see cref="SelectAsync"/> has never successfully opened a source — #227's own scope, for
        /// clamping a requested <see cref="SeekAsync"/> position. Mirrors <see cref="CurrentSource"/>'s
        /// own lifecycle exactly (set once per successful open inside <see cref="SwitchGenerationAsync"/>,
        /// not cleared when that generation later ends/faults) rather than reading through <c>_current</c>
        /// — <c>_current</c> itself goes back to <see langword="null"/> the moment a generation finishes
        /// on its own (see <see cref="SuperviseGenerationAsync"/>), which would otherwise make this go
        /// <see langword="null"/> right when a caller needs it most: clamping a <see cref="SeekAsync"/>
        /// issued after the video has already <see cref="PlayState.Ended"/>. Per
        /// <see cref="VideoStreamInfo.Duration"/>'s own remarks, <see cref="TimeSpan.Zero"/> here means
        /// "unknown/live," not "zero-length" — a caller clamping against this must treat zero the same as
        /// <see langword="null"/> (no known upper bound), not as an upper bound of zero.
        /// </summary>
        public TimeSpan? Duration => _currentStreamInfo?.Duration;

        /// <summary>
        /// This session's own <c>Video/React</c> validator/aggregator — #229's own scope. Constructed
        /// once, alongside this session, from this session's own <see cref="IMonotonicClock"/> and
        /// <see cref="VideoPlaybackSessionOptions"/> reaction settings; there is exactly one per session,
        /// unlike <see cref="VideoPlaybackSessionManager"/>'s own multi-session registry, since reactions
        /// are always scoped to one playback session's own lifetime.
        /// </summary>
        public ReactionAggregator Reactions { get; }

        /// <summary>
        /// The connection id currently authorized for this session's host-only commands, or
        /// <see langword="null"/> if no eligible subscriber remains. Safe to read from any thread
        /// without external locking.
        /// </summary>
        /// <remarks>
        /// #231's own scope, "deterministic host ownership and command authorization" — replaces #225's
        /// original temporary first-caller-wins claim. Ownership is now tied to <i>subscription</i>, not
        /// to issuing any particular command: the first eligible subscriber (the first connection to call
        /// <see cref="Subscribe"/>/<see cref="Join"/>) becomes host automatically, and when the current
        /// host disconnects (<see cref="Unsubscribe"/>), ownership reassigns deterministically to whichever
        /// remaining subscriber joined earliest — see <see cref="_subscriberJoinOrder"/>. A connection that
        /// has never subscribed can never become host merely by issuing a host-only command; see
        /// <see cref="IsHost"/>'s own remarks.
        /// </remarks>
        public string? HostConnectionId => Volatile.Read(ref _hostConnectionId);

        /// <summary>
        /// Whether <paramref name="connectionId"/> is this session's current host — the single
        /// authorization check every host-only <c>Video/*</c> pipeline (Play/Pause/Seek/Select) now
        /// centralizes on, per #231's own scope. A pure, side-effect-free read: unlike #225's original
        /// <c>TryClaimOrVerifyHost</c>, calling this can never itself grant host status — the only way to
        /// ever become host is the implicit first-eligible-subscriber assignment inside
        /// <see cref="Subscribe"/>/<see cref="Join"/>. This is also what makes host status spoof-proof: a
        /// caller cannot claim ownership by simply asserting an id here, since this method never writes
        /// <see cref="HostConnectionId"/>, only compares against it.
        /// </summary>
        public bool IsHost(string connectionId) => HostConnectionId == connectionId;

        /// <summary>
        /// The codec the current generation's audio is encoded with, or <see langword="null"/> if audio
        /// is not active (video-only/muted, or no video selected yet) — either explicitly configured via
        /// <see cref="VideoPlaybackSessionOptions.AudioEncoding"/> or auto-detected from the source's own
        /// audio codec, per that property's own remarks. This is how a caller finds out which codec was
        /// actually chosen so it can be told to clients (e.g. surfaced in a session state message) — every
        /// published <see cref="AudioFramePacket"/> also carries the same value on its own
        /// <see cref="AudioFramePacket.Encoding"/>, so a client already watching packets never strictly
        /// needs this, but a client that has not yet received one (nothing published yet, or joining
        /// before <see cref="Join"/>'s own audio bootstrap exists) does. Safe to read from any thread
        /// without external locking.
        /// </summary>
        public AudioFramePacketEncoding? AudioEncoding
        {
            get
            {
                var raw = _audioEncodingRaw;
                return raw < 0 ? null : (AudioFramePacketEncoding)raw;
            }
        }

        /// <summary>Number of viewers currently subscribed via <see cref="Subscribe"/>.</summary>
        public int ViewerCount => _subscribers.Count;

        /// <summary>
        /// Registers <paramref name="viewerId"/> for delivery, giving it its own bounded queue. A no-op
        /// if already subscribed. Never touches the decode loop or any other viewer's own queue — #220's
        /// own AC, "Keep viewer subscription changes independent from the decode loop."
        /// </summary>
        public void Subscribe(string viewerId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(viewerId);
            ObjectDisposedException.ThrowIf(_disposed, this);

            RegisterSubscriber(viewerId);
        }

        /// <summary>
        /// Registers <paramref name="viewerId"/>'s own video/audio queues (a no-op if already
        /// subscribed), records its join order, and — if no host is set yet — assigns it as this
        /// session's host, all under <see cref="_hostLock"/> so a concurrent first-join race can never
        /// produce more than one host — #231's own AC, "Concurrent joins/disconnects cannot produce
        /// multiple hosts." Shared by <see cref="Subscribe"/> and <see cref="Join"/>, which otherwise
        /// duplicated this exact registration.
        /// </summary>
        private void RegisterSubscriber(string viewerId)
        {
            // Read before either GetOrAdd below — RegisterSubscriber is documented as a no-op for an
            // already-subscribed viewer, so this is what tells RecordSubscriberJoined apart from a
            // reconnect. A benign race under concurrent first-joins for the same id (already covered by
            // this type's own concurrency tests) could over-count by at most one per race — acceptable for
            // a gauge, not worth extra synchronization on this hot-enough path.
            var isNewSubscriber = !_subscribers.ContainsKey(viewerId);

            _subscribers.GetOrAdd(viewerId, _ => new SubscriberFrameQueue<VideoFramePacket>(_options.SubscriberQueueCapacity, onFrameDropped: CreateDropCallback(VideoPlaybackMediaType.Video)));
            // Always registered, even for a session that never activates audio — an idle, never-published-to
            // queue is harmless, and keeping Subscribe a single call for "this viewer" (rather than one call
            // per track) is simpler for a caller than conditionally subscribing to audio separately.
            _audioSubscribers.GetOrAdd(viewerId, _ => new SubscriberFrameQueue<AudioFramePacket>(_options.AudioSubscriberQueueCapacity, onFrameDropped: CreateDropCallback(VideoPlaybackMediaType.Audio)));
            _subscriberJoinOrder.GetOrAdd(viewerId, _ => Interlocked.Increment(ref _nextJoinSequence));

            if (isNewSubscriber)
                _telemetry?.RecordSubscriberJoined();

            lock (_hostLock)
            {
                _hostConnectionId ??= viewerId;
            }
        }

        /// <summary>
        /// Combines this session's own caller-supplied <c>onFrameDropped</c> (still invoked verbatim, for
        /// callers that observed drops before #235) with recording it into <see cref="_telemetry"/>, tagged
        /// by <paramref name="mediaType"/> — #235's own scope, "aggregating FrameDropReason occurrences
        /// into real metrics." One combined delegate per media type rather than one per buffer/queue
        /// instance, since every video buffer/queue this session ever creates shares the same tag and
        /// likewise for audio.
        /// </summary>
        private Action<FrameDropReason> CreateDropCallback(VideoPlaybackMediaType mediaType) => reason =>
        {
            _onFrameDropped?.Invoke(reason);
            _telemetry?.RecordFrameDropped(reason, mediaType);
        };

        /// <summary>Whether <paramref name="viewerId"/> is currently subscribed via <see cref="Subscribe"/>/<see cref="Join"/> — #229's own scope, "Validate viewer/session membership," which needs a way to check membership without the side effect both of those calls otherwise have.</summary>
        public bool IsSubscribed(string viewerId) => _subscribers.ContainsKey(viewerId);

        /// <summary>
        /// Removes and disposes <paramref name="viewerId"/>'s own video and audio queues. Returns
        /// <see langword="false"/> if it was not subscribed. If <paramref name="viewerId"/> was this
        /// session's host, also reassigns <see cref="HostConnectionId"/> deterministically to whichever
        /// remaining subscriber joined earliest, or clears it to <see langword="null"/> if none remain —
        /// #231's own AC, "Reassignment is deterministic and occurs once per departure." A caller that
        /// needs to know whether reassignment actually happened (e.g. to decide whether to broadcast an
        /// updated host) can simply compare <see cref="HostConnectionId"/> before and after calling this.
        /// </summary>
        public bool Unsubscribe(string viewerId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(viewerId);

            if (_audioSubscribers.TryRemove(viewerId, out var audioQueue))
                audioQueue.Dispose();

            _subscriberJoinOrder.TryRemove(viewerId, out _);

            lock (_hostLock)
            {
                if (_hostConnectionId == viewerId)
                    _hostConnectionId = _subscriberJoinOrder.OrderBy(kvp => kvp.Value).Select(kvp => (string?)kvp.Key).FirstOrDefault();
            }

            if (!_subscribers.TryRemove(viewerId, out var queue))
                return false;

            queue.Dispose();
            _telemetry?.RecordSubscriberLeft();
            return true;
        }

        /// <summary>Dequeues the oldest video packet queued for <paramref name="viewerId"/>, if any and if it is subscribed.</summary>
        public bool TryDequeue(string viewerId, out VideoFramePacket? packet)
        {
            if (_subscribers.TryGetValue(viewerId, out var queue))
                return queue.TryDequeue(out packet);

            packet = null;
            return false;
        }

        /// <summary>Dequeues the oldest audio packet queued for <paramref name="viewerId"/>, if any and if it is subscribed. Always empty for a session running without audio — see this type's own remarks.</summary>
        public bool TryDequeueAudio(string viewerId, out AudioFramePacket? packet)
        {
            if (_audioSubscribers.TryGetValue(viewerId, out var queue))
                return queue.TryDequeue(out packet);

            packet = null;
            return false;
        }

        /// <summary>
        /// Subscribes <paramref name="viewerId"/> exactly as <see cref="Subscribe"/> does — creating no
        /// decoder or timeline of its own, just registering their queue — and, atomically with that
        /// subscription, unicasts whatever frame was most recently published (if any, and if it still
        /// belongs to the current epoch) directly into their own queue so they start at the current live
        /// position rather than frame 0. See this type's own remarks on how the atomicity is achieved and
        /// why it is necessary. Safe to call while <see cref="State"/> is <see cref="PlayState.Paused"/>
        /// or <see cref="PlayState.Buffering"/> — the bootstrap frame is simply whatever was last
        /// published, which naturally stays fixed while paused — #223's own AC, "Paused joins display the
        /// paused frame."
        /// </summary>
        public LateJoinSnapshot Join(string viewerId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(viewerId);
            ObjectDisposedException.ThrowIf(_disposed, this);

            lock (_publishGate)
            {
                RegisterSubscriber(viewerId);

                // Bootstrapped under the very same lock/critical-section as the video frame below, for
                // exactly the same reason — see this type's own remarks on Join's atomicity. Audio has no
                // snapshot fields of its own to return (video's own Epoch/MediaPosition/SyncTime already
                // describe "the position" for both tracks at once), so this is otherwise a pure side effect.
                if (_lastPublishedAudioPacket is { } audioPacket && _audioSubscribers.TryGetValue(viewerId, out var audioQueue))
                    audioQueue.Enqueue(audioPacket);

                if (_lastPublishedFrame is { } frame && _subscribers.TryGetValue(viewerId, out var queue))
                    queue.Enqueue(frame);

                return BuildSnapshot();
            }
        }

        /// <summary>
        /// Returns the same position/frame/state data <see cref="Join"/> would bootstrap a new subscriber
        /// with, but without any subscription side effect — for callers (like <c>Video/Pause</c>, #226)
        /// that need to read "what is currently playing" without joining as a viewer. Uses the same
        /// <c>_publishGate</c>-guarded snapshot as <see cref="Join"/> for the same atomicity reason: see
        /// that method's own remarks.
        /// </summary>
        public LateJoinSnapshot PeekSnapshot()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            lock (_publishGate)
            {
                return BuildSnapshot();
            }
        }

        /// <summary>Must be called with <see cref="_publishGate"/> already held — see <see cref="Join"/> and <see cref="PeekSnapshot"/>, its only two callers.</summary>
        private LateJoinSnapshot BuildSnapshot()
        {
            var frame = _lastPublishedFrame;
            if (frame is null)
            {
                return new LateJoinSnapshot
                {
                    State = _state,
                    Epoch = Epoch,
                    HasBootstrapFrame = false,
                    FrameNumber = 0,
                    MediaPosition = TimeSpan.Zero,
                    SyncTime = TimeSpan.Zero
                };
            }

            return new LateJoinSnapshot
            {
                State = _state,
                Epoch = frame.Epoch,
                HasBootstrapFrame = true,
                FrameNumber = frame.FrameNumber,
                MediaPosition = frame.PresentationTimestamp,
                SyncTime = frame.DisplayTime
            };
        }

        /// <summary>
        /// Opens <paramref name="source"/> and starts decoding/publishing from <paramref name="startPosition"/>,
        /// first stopping and disposing whichever generation (if any) was previously active — calling
        /// this again while already playing is how a caller switches to a different video.
        /// </summary>
        public Task SelectAsync(VideoSource source, TimeSpan startPosition = default, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            return SwitchGenerationAsync(source, startPosition, cancellationToken);
        }

        /// <summary>Re-seeks the currently selected video to <paramref name="position"/>. Requires a video already selected via <see cref="SelectAsync"/>.</summary>
        public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
        {
            var source = _currentSource ?? throw new InvalidOperationException($"{nameof(SeekAsync)} requires a video already selected via {nameof(SelectAsync)}.");
            return SwitchGenerationAsync(source, position, cancellationToken);
        }

        /// <summary>
        /// #232's own scope, "Avoid holding locks across arbitrary user/network callbacks": runs in three
        /// phases rather than one long <see cref="_lifecycleLock"/>-held critical section, since opening a
        /// source (<see cref="IVideoFrameSource.OpenAsync"/>) can take real, externally-controlled
        /// wall-clock time (network, disk, decoder handshake) and previously blocked every other command
        /// against this session for that whole duration. Phase 1 (locked) does only fast, local
        /// mutations and captures this attempt's own epoch. Phase 2 (unlocked) does the slow work — stop
        /// the previous generation, open the new source. Phase 3 (locked again, briefly) re-validates
        /// nothing superseded this attempt while it was unlocked, and only then activates the new
        /// generation.
        /// <para/>
        /// This preserves the same "last Phase-1-order wins" determinism concurrent
        /// <see cref="SelectAsync"/>/<see cref="SeekAsync"/> calls already had when this was one
        /// single-locked critical section (established and tested since #227): two concurrent attempts
        /// both pass Phase 1 sequentially (still fully serialized), so whichever runs Phase 1 <i>second</i>
        /// always captures the higher epoch; whichever captured the <i>lower</i> epoch always loses in
        /// Phase 3, even if its own Phase 2 (opening) happened to finish first. Only one attempt's
        /// Phase-1 call ever captures a non-null <c>previous</c> generation (whichever ran first observes
        /// the real reference; every later concurrent attempt's own Phase 1 finds <c>_current</c> already
        /// cleared to <see langword="null"/> and so has nothing of its own to stop) — <c>previous</c> is
        /// always stopped exactly once regardless of how many attempts race through afterward.
        /// </summary>
        private async Task SwitchGenerationAsync(VideoSource source, TimeSpan startPosition, CancellationToken cancellationToken)
        {
            Generation? previous;
            int myEpoch;

            await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                previous = _current;
                _current = null;
                SetState(PlayState.Loading);

                Interlocked.Increment(ref _epoch);
                myEpoch = Epoch;
                Interlocked.Exchange(ref _nextFrameNumber, 0);
                Interlocked.Exchange(ref _nextAudioPacketNumber, 0);
                _audioEncodingRaw = -1;
                _currentSource = source;

                // A join racing this switch must never bootstrap a frame from the epoch being replaced —
                // #223's own AC, "Snapshot and bootstrap belong to the same epoch."
                lock (_publishGate)
                {
                    _lastPublishedFrame = null;
                    _lastPublishedAudioPacket = null;
                }
            }
            finally
            {
                _lifecycleLock.Release();
            }

            // Phase 2 — unlocked. Stopping the previous generation and opening the new source can both
            // take real time (a stuck decode task's own cancellation responsiveness; real I/O), and
            // neither needs _lifecycleLock held to be safe: this generation's own eventual activation (or
            // abandonment) is re-validated against `myEpoch` under the lock again in Phase 3 below.
            if (previous is not null)
                await StopGenerationAsync(previous).ConfigureAwait(false);

            var newSource = _sourceFactory();
            VideoStreamInfo streamInfo;
            try
            {
                streamInfo = await newSource.OpenAsync(source, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await newSource.DisposeAsync().ConfigureAwait(false);

                // #235's own AC, "Emit structured state-transition and source failure logs with
                // redaction": logs SessionId/Epoch and the exception only — never `source` itself.
                // FfmpegVideoFrameSource's own VideoFrameSourceException messages already never
                // interpolate VideoSource.Location (only a generic FFmpeg error description), so ex.Message
                // is safe to log verbatim here without any extra scrubbing.
                _telemetry?.RecordSessionFailure("open");
                _logger?.LogError(ex, "VideoPlaybackSession {SessionId} failed to open a source for epoch {Epoch}", SessionId, myEpoch);

                await _lifecycleLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    // Only fault if nothing has already superseded this attempt — a concurrent
                    // Select/Seek that raced ahead and already started its own newer generation must
                    // never have its state clobbered by this now-abandoned attempt's own failure.
                    if (!_disposed && Epoch == myEpoch)
                        SetState(PlayState.Faulted);
                }
                finally { _lifecycleLock.Release(); }

                throw;
            }

            var pacer = new FramePacer(_clock, _options.PlaybackRate);
            pacer.Start(startPosition);

            var generation = new Generation
            {
                Epoch = myEpoch,
                Source = newSource,
                Buffer = new DecodedFrameBuffer(_options.DecodeBufferCapacity, CreateDropCallback(VideoPlaybackMediaType.Video)),
                Pacer = pacer,
                Cts = CancellationTokenSource.CreateLinkedTokenSource(_hostShutdownToken)
            };

            // Phase 3 — locked again, briefly.
            await _lifecycleLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_disposed || Epoch != myEpoch)
                {
                    // Superseded (or disposed) while unlocked in Phase 2 — this attempt lost the race.
                    // Abandon it: dispose what was just opened rather than ever making it _current, and
                    // never touch _state — whichever newer attempt is active now (or the disposal itself)
                    // owns state, not this one.
                    await newSource.DisposeAsync().ConfigureAwait(false);
                    generation.Cts.Dispose();
                    return;
                }

                _currentStreamInfo = streamInfo;

                // TryStartAudioAsync's own await audioSource.OpenAsync(...) still runs inside this
                // Phase-3 lock, deliberately not given the same three-phase treatment as the video open
                // above — #232's own scope. Unlike video, this call only ever runs once Phase 3 has
                // already confirmed this generation is the winning, non-superseded attempt, so there is
                // no correctness question here, only a possible (typically short) added latency for other
                // commands during that one call. Audio's own open failure is already fault-tolerant (see
                // this type's own remarks on audio always being best-effort relative to video), so a slow
                // audio open blocking other commands briefly is judged an acceptable, secondary-path cost
                // rather than one that justifies a second full split-phase treatment (which would itself
                // need its own re-validation step, for comparatively little benefit).
                await TryStartAudioAsync(generation, startPosition, cancellationToken).ConfigureAwait(false);

                generation.DecodeTask = RunDecodeLoopAsync(generation, startPosition, generation.Cts.Token);
                generation.PublishTask = RunPublishLoopAsync(generation, generation.Cts.Token);

                _current = generation;
                SetState(PlayState.Playing);

                _ = SuperviseGenerationAsync(generation);
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        /// <summary>
        /// Best-effort: opens an audio source for this generation and starts its own decode/publish
        /// loops if audio is enabled, a factory was supplied, and the source actually has audio to open
        /// — leaves <see cref="Generation.AudioSource"/> null (video-only) on any failure, including "no
        /// audio track," rather than letting an audio problem fault the whole generation. See this
        /// type's own remarks.
        /// </summary>
        private async Task TryStartAudioAsync(Generation generation, TimeSpan startPosition, CancellationToken cancellationToken)
        {
            if (!_options.EnableAudio || _audioSourceFactory is null)
                return;

            var audioSource = _audioSourceFactory();
            try
            {
                var audioStreamInfo = await audioSource.OpenAsync(_currentSource!, cancellationToken).ConfigureAwait(false);
                var resolvedEncoding = ResolveAudioEncoding(_options.AudioEncoding, audioStreamInfo.SourceCodecName);

                generation.AudioSource = audioSource;
                generation.AudioEncoding = resolvedEncoding;
                generation.AudioBuffer = new DecodedAudioBuffer(_options.AudioDecodeBufferCapacity, CreateDropCallback(VideoPlaybackMediaType.Audio));
                generation.AudioEncoder = _audioEncoderFactory(audioStreamInfo.SampleRate, audioStreamInfo.Channels, resolvedEncoding);
                generation.AudioDecodeTask = RunAudioDecodeLoopAsync(generation, startPosition, generation.Cts.Token);
                generation.AudioPublishTask = RunAudioPublishLoopAsync(generation, generation.Cts.Token);

                _audioEncodingRaw = (int)resolvedEncoding;
            }
            catch (VideoFrameSourceException)
            {
                // No audio track, or the audio decoder itself couldn't be opened — video-only for this
                // generation. Not a caller-visible failure and not a session fault; see this type's own
                // remarks on why audio is always best-effort relative to video.
                await audioSource.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>See <see cref="VideoPlaybackSessionOptions.AudioEncoding"/>'s own remarks for the auto-detect heuristic this implements.</summary>
        private static AudioFramePacketEncoding ResolveAudioEncoding(AudioFramePacketEncoding? configured, string sourceCodecName) =>
            configured ?? (string.Equals(sourceCodecName, "aac", StringComparison.OrdinalIgnoreCase) ? AudioFramePacketEncoding.Aac : AudioFramePacketEncoding.Opus);

        /// <summary>Freezes the shared timeline for every subscriber at once. Requires a video already selected.</summary>
        public async Task PauseAsync(CancellationToken cancellationToken = default)
        {
            await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                var current = _current ?? throw new InvalidOperationException($"{nameof(PauseAsync)} requires a video already selected via {nameof(SelectAsync)}.");

                current.Pacer.Pause();
                SetState(PlayState.Paused);
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        /// <summary>Resumes a paused shared timeline. Requires a video already selected.</summary>
        public async Task ResumeAsync(CancellationToken cancellationToken = default)
        {
            await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                var current = _current ?? throw new InvalidOperationException($"{nameof(ResumeAsync)} requires a video already selected via {nameof(SelectAsync)}.");

                current.Pacer.Resume();
                SetState(PlayState.Playing);
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        /// <summary>
        /// Stops playback normally and disposes media resources. Subscribers remain registered until
        /// <see cref="Unsubscribe"/>/<see cref="DisposeAsync"/>.
        /// </summary>
        /// <remarks>
        /// #232's own scope, "no deadlocks occur during cancellation/disposal": <see cref="StopGenerationAsync"/>
        /// runs outside <see cref="_lifecycleLock"/> (mirroring <see cref="DisposeAsync"/>'s own
        /// already-correct shape, which this brings <see cref="EndAsync"/> in line with) — a decode/publish
        /// task that responds to cancellation slowly (or never) can no longer wedge the lock itself,
        /// blocking every other command against this session; only this one caller's own await might still
        /// individually take a while.
        /// </remarks>
        public async Task EndAsync(CancellationToken cancellationToken = default)
        {
            Generation? current;

            await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                current = _current;
                _current = null;
                SetState(PlayState.Ended);
            }
            finally
            {
                _lifecycleLock.Release();
            }

            if (current is not null)
                await StopGenerationAsync(current).ConfigureAwait(false);
        }

        /// <summary>Cancels and disposes all media work and every subscriber's own queue. Safe to call more than once.</summary>
        public async ValueTask DisposeAsync()
        {
            // Fast pre-check before touching _lifecycleLock at all — once the first successful call
            // below has disposed it, a later call must never call WaitAsync() on it again.
            if (_disposed)
                return;

            Generation? current;

            await _lifecycleLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_disposed)
                    return;

                _disposed = true;
                current = _current;
                _current = null;
            }
            finally
            {
                _lifecycleLock.Release();
            }

            if (current is not null)
                await StopGenerationAsync(current).ConfigureAwait(false);

            foreach (var viewerId in _subscribers.Keys)
                if (_subscribers.TryRemove(viewerId, out var queue))
                    queue.Dispose();

            foreach (var viewerId in _audioSubscribers.Keys)
                if (_audioSubscribers.TryRemove(viewerId, out var audioQueue))
                    audioQueue.Dispose();

            _lifecycleLock.Dispose();
        }

        // #235's own AC, "Emit structured state-transition ... logs with redaction": every transition is
        // logged with SessionId + the two PlayState values only — never the selected VideoSource.
        private void SetState(PlayState state)
        {
            var previous = _state;
            _state = state;

            if (previous != state)
                _logger?.LogInformation("VideoPlaybackSession {SessionId} transitioned from {PreviousState} to {State}", SessionId, previous, state);
        }

        private async Task RunDecodeLoopAsync(Generation generation, TimeSpan startPosition, CancellationToken token)
        {
            // Stopwatch only allocated when telemetry is actually wired up — #235's own AC, "Telemetry
            // overhead is bounded": a session with no VideoPlaybackTelemetry pays a single null check per
            // frame here, matching every other _telemetry?.-guarded call site in this type.
            var stopwatch = _telemetry is null ? null : Stopwatch.StartNew();

            await foreach (var frame in generation.Source.ReadFramesAsync(startPosition, token).ConfigureAwait(false))
            {
                if (stopwatch is not null)
                {
                    _telemetry!.RecordDecodeDuration(stopwatch.Elapsed, VideoPlaybackMediaType.Video);
                    _telemetry.RecordFrameDecoded(VideoPlaybackMediaType.Video);
                    stopwatch.Restart();
                }

                generation.Buffer.Enqueue(frame);
            }
        }

        private async Task RunPublishLoopAsync(Generation generation, CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                if (generation.Pacer.IsPaused)
                {
                    await Task.Delay(_options.PollInterval, token).ConfigureAwait(false);
                    continue;
                }

                if (generation.Buffer.TryTakeCurrent(generation.Pacer.CurrentMediaPosition, out var frame))
                {
                    using (frame)
                        PublishFrame(generation, frame!);

                    continue;
                }

                // IsCompleted (not IsCompletedSuccessfully): once decode has stopped for ANY reason —
                // success, fault, or cancellation — no more frames are ever coming, so this loop must
                // stop too once the buffer is drained. Whether that overall outcome is a fault or a
                // natural end is decided by SuperviseGenerationAsync inspecting DecodeTask itself, not by
                // this check — but that supervisor can only run once this loop actually returns, so
                // gating this solely on success (as an earlier version of this code did) would leave a
                // faulted decode loop's own publish loop polling forever and the supervisor deadlocked
                // awaiting it, and the session would never observe the fault at all.
                if (generation.DecodeTask.IsCompleted && generation.Buffer.Count == 0)
                    return;

                await Task.Delay(_options.PollInterval, token).ConfigureAwait(false);
            }
        }

        private async Task RunAudioDecodeLoopAsync(Generation generation, TimeSpan startPosition, CancellationToken token)
        {
            var stopwatch = _telemetry is null ? null : Stopwatch.StartNew();

            await foreach (var frame in generation.AudioSource!.ReadFramesAsync(startPosition, token).ConfigureAwait(false))
            {
                if (stopwatch is not null)
                {
                    _telemetry!.RecordDecodeDuration(stopwatch.Elapsed, VideoPlaybackMediaType.Audio);
                    _telemetry.RecordFrameDecoded(VideoPlaybackMediaType.Audio);
                    stopwatch.Restart();
                }

                generation.AudioBuffer!.Enqueue(frame);
            }
        }

        // Mirrors RunPublishLoopAsync's own shape exactly (same pacer-driven polling, same
        // IsCompleted-not-IsCompletedSuccessfully reasoning for recognizing "nothing more is ever
        // coming") — audio simply routes each due frame through the encoder before publishing, and one
        // decoded frame can yield zero, one, or more encoded packets (see AudioFrameEncoder's own remarks).
        private async Task RunAudioPublishLoopAsync(Generation generation, CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                if (generation.Pacer.IsPaused)
                {
                    await Task.Delay(_options.PollInterval, token).ConfigureAwait(false);
                    continue;
                }

                if (generation.AudioBuffer!.TryTakeCurrent(generation.Pacer.CurrentMediaPosition, out var frame))
                {
                    IReadOnlyList<EncodedAudioChunk> chunks;
                    if (_telemetry is null)
                    {
                        using (frame)
                            chunks = generation.AudioEncoder!.Encode(frame!);
                    }
                    else
                    {
                        var encodeStopwatch = Stopwatch.StartNew();
                        using (frame)
                            chunks = generation.AudioEncoder!.Encode(frame!);

                        _telemetry.RecordEncodeDuration(encodeStopwatch.Elapsed, VideoPlaybackMediaType.Audio);
                        _telemetry.RecordFrameEncoded(VideoPlaybackMediaType.Audio);
                    }

                    foreach (var chunk in chunks)
                        PublishAudioChunk(generation, chunk);

                    continue;
                }

                if (generation.AudioDecodeTask.IsCompleted && generation.AudioBuffer.Count == 0)
                {
                    IReadOnlyList<EncodedAudioChunk> flushed;
                    if (_telemetry is null)
                    {
                        flushed = generation.AudioEncoder!.Flush();
                    }
                    else
                    {
                        var encodeStopwatch = Stopwatch.StartNew();
                        flushed = generation.AudioEncoder!.Flush();
                        _telemetry.RecordEncodeDuration(encodeStopwatch.Elapsed, VideoPlaybackMediaType.Audio);
                    }

                    foreach (var chunk in flushed)
                        PublishAudioChunk(generation, chunk);

                    return;
                }

                await Task.Delay(_options.PollInterval, token).ConfigureAwait(false);
            }
        }

        private void PublishAudioChunk(Generation generation, EncodedAudioChunk chunk)
        {
            // #232's own AC, "Old-epoch tasks cannot publish after a successful seek/select/removal, even
            // if cancellation is observed late." The only thing that stops a superseded generation's own
            // publish loop is its CancellationTokenSource being observed — but that loop checks
            // cancellation once per iteration, before dequeuing/publishing a chunk that was already
            // sitting in the buffer, so a cancel racing that narrow window would otherwise still let this
            // method run. generation.Epoch is fixed at creation (never the live, possibly-already-advanced
            // session Epoch), so this comparison is the actual guard against a stale publish, not the
            // token check alone.
            if (generation.Epoch != Epoch)
                return;

            var publishStopwatch = _telemetry is null ? null : Stopwatch.StartNew();

            var schedule = generation.Pacer.ComputeSchedule(chunk.PresentationTimestamp);
            var streamInfo = generation.AudioSource!.StreamInfo!;

            var packet = new AudioFramePacket
            {
                SessionId = SessionId,
                Epoch = generation.Epoch,
                PacketNumber = Interlocked.Increment(ref _nextAudioPacketNumber) - 1,
                PresentationTimestamp = chunk.PresentationTimestamp,
                Duration = chunk.Duration,
                DisplayTime = schedule.DueElapsed,
                SampleRate = streamInfo.SampleRate,
                Channels = streamInfo.Channels,
                Encoding = generation.AudioEncoding!.Value,
                Payload = chunk.Payload
            };

            lock (_publishGate)
            {
                // Re-checked inside the gate too: Epoch could have advanced in the narrow window between
                // the check above and acquiring this lock (a switch could complete in between) — this
                // closes that last sliver rather than leaving it to chance.
                if (generation.Epoch != Epoch)
                    return;

                _lastPublishedAudioPacket = packet;

                foreach (var subscriber in _audioSubscribers.Values)
                    subscriber.Enqueue(packet);
            }

            if (_telemetry is not null)
            {
                var pacingDrift = generation.Pacer.GetPacingError(chunk.PresentationTimestamp);
                publishStopwatch!.Stop();

                _telemetry.RecordFramePublished(VideoPlaybackMediaType.Audio, packet.Payload.Length);
                _telemetry.RecordPacingDrift(pacingDrift, VideoPlaybackMediaType.Audio);
                _telemetry.RecordPublishLatency(publishStopwatch.Elapsed, VideoPlaybackMediaType.Audio);

                using var activity = _telemetry.StartSampledFrameActivity(
                    VideoPlaybackMediaType.Audio, SessionId, packet.Epoch, packet.PacketNumber,
                    chunk.PresentationTimestamp, pacingDrift, publishStopwatch.Elapsed, packet.Payload.Length);
            }
        }

        private void PublishFrame(Generation generation, DecodedVideoFrame frame)
        {
            // See PublishAudioChunk's own remarks on why this check exists and why it must use
            // generation.Epoch (fixed at creation), not the live session Epoch.
            if (generation.Epoch != Epoch)
                return;

            // Spans the whole publish operation (encode + fan-out below) — #235's own "publish latency"
            // signal, deliberately distinct from decode/encode duration since it also captures fan-out cost,
            // which grows with subscriber count.
            var publishStopwatch = _telemetry is null ? null : Stopwatch.StartNew();

            var schedule = generation.Pacer.ComputeSchedule(frame.PresentationTimestamp);

            ReadOnlyMemory<byte> payload;
            if (_telemetry is null)
            {
                payload = _encodeFrame(frame);
            }
            else
            {
                var encodeStopwatch = Stopwatch.StartNew();
                payload = _encodeFrame(frame);
                _telemetry.RecordEncodeDuration(encodeStopwatch.Elapsed, VideoPlaybackMediaType.Video);
                _telemetry.RecordFrameEncoded(VideoPlaybackMediaType.Video);
            }

            var packet = new VideoFramePacket
            {
                SessionId = SessionId,
                Epoch = generation.Epoch,
                FrameNumber = Interlocked.Increment(ref _nextFrameNumber) - 1,
                PresentationTimestamp = frame.PresentationTimestamp,
                Duration = frame.Duration,
                DisplayTime = schedule.DueElapsed,
                Width = frame.Width,
                Height = frame.Height,
                Encoding = _options.Encoding,
                Payload = payload
            };

            // Recording "this is now the last published frame" and delivering it to every currently
            // subscribed viewer must happen as one atomic unit relative to Join's own "subscribe, then
            // unicast the last published frame" — see this type's own remarks.
            lock (_publishGate)
            {
                // Re-checked inside the gate too — see PublishAudioChunk's own remarks.
                if (generation.Epoch != Epoch)
                    return;

                _lastPublishedFrame = packet;

                foreach (var subscriber in _subscribers.Values)
                    subscriber.Enqueue(packet);
            }

            if (_telemetry is not null)
            {
                var pacingDrift = generation.Pacer.GetPacingError(frame.PresentationTimestamp);
                publishStopwatch!.Stop();

                _telemetry.RecordFramePublished(VideoPlaybackMediaType.Video, packet.Payload.Length);
                _telemetry.RecordPacingDrift(pacingDrift, VideoPlaybackMediaType.Video);
                _telemetry.RecordPublishLatency(publishStopwatch.Elapsed, VideoPlaybackMediaType.Video);

                using var activity = _telemetry.StartSampledFrameActivity(
                    VideoPlaybackMediaType.Video, SessionId, packet.Epoch, packet.FrameNumber,
                    frame.PresentationTimestamp, pacingDrift, publishStopwatch.Elapsed, packet.Payload.Length);
            }
        }

        /// <summary>Explicitly stops one generation (cancel, await every loop — video and audio alike, dispose once) — used by every lifecycle call that tears one down on purpose.</summary>
        private static async Task StopGenerationAsync(Generation generation)
        {
            // A generation that finished on its own (natural end/fault) just before this call can have
            // already disposed its own Cts via SuperviseGenerationAsync's own EnsureCleanedUpOnceAsync
            // call, racing this explicit stop for the same generation — #232's own "no deadlocks/crashes
            // during cancellation" scope. Already-disposed means its tasks have already stopped on their
            // own, so there is nothing left to cancel; safe to ignore rather than let it escape.
            try { generation.Cts.Cancel(); }
            catch (ObjectDisposedException) { }

            try { await generation.DecodeTask.ConfigureAwait(false); } catch { /* observed below or by the supervisor */ }
            try { await generation.PublishTask.ConfigureAwait(false); } catch { }
            try { await generation.AudioDecodeTask.ConfigureAwait(false); } catch { }
            try { await generation.AudioPublishTask.ConfigureAwait(false); } catch { }

            await generation.EnsureCleanedUpOnceAsync(DisposeGenerationResourcesAsync).ConfigureAwait(false);
        }

        /// <summary>
        /// Watches one generation's loops to completion and, if nothing else already stopped it on
        /// purpose, transitions this session to <see cref="PlayState.Faulted"/> or
        /// <see cref="PlayState.Ended"/> — but only while this generation is still the active one, so a
        /// stale fault/end from an already-superseded generation can never clobber a newer generation's
        /// state.
        /// </summary>
        private async Task SuperviseGenerationAsync(Generation generation)
        {
            Exception? fault = null;

            try { await generation.DecodeTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { fault = ex; }

            try { await generation.PublishTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { fault ??= ex; }

            // Audio's own outcome never contributes to `fault` — see this type's own remarks on why an
            // audio failure never faults the whole session. Still awaited here (not fire-and-forget)
            // purely so the cleanup below never disposes audio resources while its own loops might still
            // be running.
            try { await generation.AudioDecodeTask.ConfigureAwait(false); } catch { }
            try { await generation.AudioPublishTask.ConfigureAwait(false); } catch { }

            var wasExplicitlyStopped = generation.Cts.IsCancellationRequested;

            await generation.EnsureCleanedUpOnceAsync(DisposeGenerationResourcesAsync).ConfigureAwait(false);

            if (wasExplicitlyStopped)
                return;

            await _lifecycleLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!ReferenceEquals(_current, generation))
                    return; // superseded before this fault/end could be recorded — stale, ignore.

                _current = null;

                if (fault is not null)
                {
                    _fault = fault;
                    SetState(PlayState.Faulted);

                    // See SwitchGenerationAsync's own catch block for why logging `fault` verbatim here
                    // never leaks a source URL/path.
                    _telemetry?.RecordSessionFailure("playback");
                    _logger?.LogError(fault, "VideoPlaybackSession {SessionId} faulted at epoch {Epoch}", SessionId, generation.Epoch);
                }
                else
                {
                    SetState(PlayState.Ended);
                }
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        private static async Task DisposeGenerationResourcesAsync(Generation generation)
        {
            generation.Buffer.Dispose();
            await generation.Source.DisposeAsync().ConfigureAwait(false);

            // Never flushed here — see AudioFrameEncoder.Flush's own remarks; whatever trailing packets a
            // clean end-of-stream still wanted are already handled by RunAudioPublishLoopAsync itself
            // before it returns. A generation being torn down for any other reason (seek/select/end/
            // dispose) has no one left to deliver a final flush to anyway.
            generation.AudioEncoder?.Dispose();
            generation.AudioBuffer?.Dispose();
            if (generation.AudioSource is not null)
                await generation.AudioSource.DisposeAsync().ConfigureAwait(false);

            generation.Cts.Dispose();
        }

        /// <summary>One selected video's own source/buffer/pacer/loop-pair, plus its own optional audio counterpart. See this type's own remarks on generations and on audio.</summary>
        private sealed class Generation
        {
            private readonly TaskCompletionSource _cleanupCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private int _cleanedUp;

            /// <summary>
            /// This generation's own fixed epoch, captured once at creation from the session's live
            /// <c>Epoch</c> at that moment — #232's own scope. Never re-read from the session afterward:
            /// PublishFrame/PublishAudioChunk compare against this fixed value (not the session's live
            /// Epoch, which may have already advanced past it) to detect and refuse a stale publish.
            /// </summary>
            public required int Epoch { get; init; }

            public required IVideoFrameSource Source { get; init; }
            public required DecodedFrameBuffer Buffer { get; init; }
            public required FramePacer Pacer { get; init; }
            public required CancellationTokenSource Cts { get; init; }
            public Task DecodeTask { get; set; } = Task.CompletedTask;
            public Task PublishTask { get; set; } = Task.CompletedTask;

            /// <summary>Non-null only when this generation's audio pipeline actually activated — see <c>TryStartAudioAsync</c>.</summary>
            public IAudioFrameSource? AudioSource { get; set; }
            public AudioFramePacketEncoding? AudioEncoding { get; set; }
            public DecodedAudioBuffer? AudioBuffer { get; set; }
            public IAudioEncoder? AudioEncoder { get; set; }
            public Task AudioDecodeTask { get; set; } = Task.CompletedTask;
            public Task AudioPublishTask { get; set; } = Task.CompletedTask;

            /// <summary>
            /// Runs <paramref name="cleanup"/> exactly once for this generation regardless of which of
            /// {an explicit stop, the supervisor} calls this first — the other caller awaits the first
            /// one's own completion instead of running it again or racing a double-dispose.
            /// </summary>
            public async Task EnsureCleanedUpOnceAsync(Func<Generation, Task> cleanup)
            {
                if (Interlocked.CompareExchange(ref _cleanedUp, 1, 0) == 0)
                {
                    try
                    {
                        await cleanup(this).ConfigureAwait(false);
                    }
                    finally
                    {
                        _cleanupCompleted.TrySetResult();
                    }
                }
                else
                {
                    await _cleanupCompleted.Task.ConfigureAwait(false);
                }
            }
        }
    }
}
