using System.Collections.Concurrent;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
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
        private readonly CancellationToken _hostShutdownToken;
        private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
        private readonly ConcurrentDictionary<string, SubscriberFrameQueue<VideoFramePacket>> _subscribers = new();
        private readonly ConcurrentDictionary<string, SubscriberFrameQueue<AudioFramePacket>> _audioSubscribers = new();

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
            Func<int, int, AudioFramePacketEncoding, IAudioEncoder>? audioEncoderFactory = null)
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
            _hostShutdownToken = hostShutdownToken;
        }

        /// <summary>Identifies this session — the same value every <see cref="VideoFramePacket.SessionId"/> it publishes carries.</summary>
        public string SessionId { get; }

        /// <summary>This session's current lifecycle state. Safe to read from any thread without external locking.</summary>
        public PlayState State => _state;

        /// <summary>The exception a fault transitioned this session with, or <see langword="null"/> if it never faulted.</summary>
        public Exception? Fault => _fault;

        /// <summary>The current stream epoch — incremented by every <see cref="SelectAsync"/>/<see cref="SeekAsync"/> call that actually starts a new generation.</summary>
        public int Epoch => Volatile.Read(ref _epoch);

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

            _subscribers.GetOrAdd(viewerId, _ => new SubscriberFrameQueue<VideoFramePacket>(_options.SubscriberQueueCapacity, onFrameDropped: _onFrameDropped));
            // Always registered, even for a session that never activates audio — an idle, never-published-to
            // queue is harmless, and keeping Subscribe a single call for "this viewer" (rather than one call
            // per track) is simpler for a caller than conditionally subscribing to audio separately.
            _audioSubscribers.GetOrAdd(viewerId, _ => new SubscriberFrameQueue<AudioFramePacket>(_options.AudioSubscriberQueueCapacity, onFrameDropped: _onFrameDropped));
        }

        /// <summary>Removes and disposes <paramref name="viewerId"/>'s own video and audio queues. Returns <see langword="false"/> if it was not subscribed.</summary>
        public bool Unsubscribe(string viewerId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(viewerId);

            if (_audioSubscribers.TryRemove(viewerId, out var audioQueue))
                audioQueue.Dispose();

            if (!_subscribers.TryRemove(viewerId, out var queue))
                return false;

            queue.Dispose();
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
                _subscribers.GetOrAdd(viewerId, _ => new SubscriberFrameQueue<VideoFramePacket>(_options.SubscriberQueueCapacity, onFrameDropped: _onFrameDropped));
                _audioSubscribers.GetOrAdd(viewerId, _ => new SubscriberFrameQueue<AudioFramePacket>(_options.AudioSubscriberQueueCapacity, onFrameDropped: _onFrameDropped));

                // Bootstrapped under the very same lock/critical-section as the video frame below, for
                // exactly the same reason — see this type's own remarks on Join's atomicity. Audio has no
                // snapshot fields of its own to return (video's own Epoch/MediaPosition/SyncTime already
                // describe "the position" for both tracks at once), so this is otherwise a pure side effect.
                if (_lastPublishedAudioPacket is { } audioPacket && _audioSubscribers.TryGetValue(viewerId, out var audioQueue))
                    audioQueue.Enqueue(audioPacket);

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

                if (_subscribers.TryGetValue(viewerId, out var queue))
                    queue.Enqueue(frame);

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

        private async Task SwitchGenerationAsync(VideoSource source, TimeSpan startPosition, CancellationToken cancellationToken)
        {
            await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                var previous = _current;
                _current = null;
                SetState(PlayState.Loading);

                if (previous is not null)
                    await StopGenerationAsync(previous).ConfigureAwait(false);

                Interlocked.Increment(ref _epoch);
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

                var newSource = _sourceFactory();
                try
                {
                    await newSource.OpenAsync(source, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    await newSource.DisposeAsync().ConfigureAwait(false);
                    SetState(PlayState.Faulted);
                    throw;
                }

                var pacer = new FramePacer(_clock, _options.PlaybackRate);
                pacer.Start(startPosition);

                var generation = new Generation
                {
                    Source = newSource,
                    Buffer = new DecodedFrameBuffer(_options.DecodeBufferCapacity, _onFrameDropped),
                    Pacer = pacer,
                    Cts = CancellationTokenSource.CreateLinkedTokenSource(_hostShutdownToken)
                };

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
                generation.AudioBuffer = new DecodedAudioBuffer(_options.AudioDecodeBufferCapacity, _onFrameDropped);
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

        /// <summary>Stops playback normally and disposes media resources. Subscribers remain registered until <see cref="Unsubscribe"/>/<see cref="DisposeAsync"/>.</summary>
        public async Task EndAsync(CancellationToken cancellationToken = default)
        {
            await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                var current = _current;
                _current = null;

                if (current is not null)
                    await StopGenerationAsync(current).ConfigureAwait(false);

                SetState(PlayState.Ended);
            }
            finally
            {
                _lifecycleLock.Release();
            }
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

        private void SetState(PlayState state) => _state = state;

        private async Task RunDecodeLoopAsync(Generation generation, TimeSpan startPosition, CancellationToken token)
        {
            await foreach (var frame in generation.Source.ReadFramesAsync(startPosition, token).ConfigureAwait(false))
                generation.Buffer.Enqueue(frame);
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
            await foreach (var frame in generation.AudioSource!.ReadFramesAsync(startPosition, token).ConfigureAwait(false))
                generation.AudioBuffer!.Enqueue(frame);
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
                    using (frame)
                        chunks = generation.AudioEncoder!.Encode(frame!);

                    foreach (var chunk in chunks)
                        PublishAudioChunk(generation, chunk);

                    continue;
                }

                if (generation.AudioDecodeTask.IsCompleted && generation.AudioBuffer.Count == 0)
                {
                    foreach (var chunk in generation.AudioEncoder!.Flush())
                        PublishAudioChunk(generation, chunk);

                    return;
                }

                await Task.Delay(_options.PollInterval, token).ConfigureAwait(false);
            }
        }

        private void PublishAudioChunk(Generation generation, EncodedAudioChunk chunk)
        {
            var schedule = generation.Pacer.ComputeSchedule(chunk.PresentationTimestamp);
            var streamInfo = generation.AudioSource!.StreamInfo!;

            var packet = new AudioFramePacket
            {
                SessionId = SessionId,
                Epoch = Epoch,
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
                _lastPublishedAudioPacket = packet;

                foreach (var subscriber in _audioSubscribers.Values)
                    subscriber.Enqueue(packet);
            }
        }

        private void PublishFrame(Generation generation, DecodedVideoFrame frame)
        {
            var schedule = generation.Pacer.ComputeSchedule(frame.PresentationTimestamp);

            var packet = new VideoFramePacket
            {
                SessionId = SessionId,
                Epoch = Epoch,
                FrameNumber = Interlocked.Increment(ref _nextFrameNumber) - 1,
                PresentationTimestamp = frame.PresentationTimestamp,
                Duration = frame.Duration,
                DisplayTime = schedule.DueElapsed,
                Width = frame.Width,
                Height = frame.Height,
                Encoding = _options.Encoding,
                Payload = _encodeFrame(frame)
            };

            // Recording "this is now the last published frame" and delivering it to every currently
            // subscribed viewer must happen as one atomic unit relative to Join's own "subscribe, then
            // unicast the last published frame" — see this type's own remarks.
            lock (_publishGate)
            {
                _lastPublishedFrame = packet;

                foreach (var subscriber in _subscribers.Values)
                    subscriber.Enqueue(packet);
            }
        }

        /// <summary>Explicitly stops one generation (cancel, await every loop — video and audio alike, dispose once) — used by every lifecycle call that tears one down on purpose.</summary>
        private static async Task StopGenerationAsync(Generation generation)
        {
            generation.Cts.Cancel();

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
