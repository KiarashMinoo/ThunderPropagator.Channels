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
    /// </remarks>
    public sealed class VideoPlaybackSession : IAsyncDisposable
    {
        private readonly Func<IVideoFrameSource> _sourceFactory;
        private readonly IMonotonicClock _clock;
        private readonly Func<DecodedVideoFrame, ReadOnlyMemory<byte>> _encodeFrame;
        private readonly VideoPlaybackSessionOptions _options;
        private readonly Action<FrameDropReason>? _onFrameDropped;
        private readonly CancellationToken _hostShutdownToken;
        private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
        private readonly ConcurrentDictionary<string, SubscriberFrameQueue<VideoFramePacket>> _subscribers = new();

        private volatile PlayState _state = PlayState.Loading;
        private volatile Exception? _fault;
        private Generation? _current;
        private VideoSource? _currentSource;
        private int _epoch;
        private long _nextFrameNumber;
        private bool _disposed;

        public VideoPlaybackSession(
            string sessionId,
            Func<IVideoFrameSource> sourceFactory,
            IMonotonicClock clock,
            VideoPlaybackSessionOptions? options = null,
            Func<DecodedVideoFrame, ReadOnlyMemory<byte>>? encodeFrame = null,
            Action<FrameDropReason>? onFrameDropped = null,
            CancellationToken hostShutdownToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ArgumentNullException.ThrowIfNull(sourceFactory);
            ArgumentNullException.ThrowIfNull(clock);

            _options = options ?? new VideoPlaybackSessionOptions();
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_options.DecodeBufferCapacity, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_options.SubscriberQueueCapacity, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_options.PlaybackRate, 0.0);

            SessionId = sessionId;
            _sourceFactory = sourceFactory;
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
        }

        /// <summary>Removes and disposes <paramref name="viewerId"/>'s own queue. Returns <see langword="false"/> if it was not subscribed.</summary>
        public bool Unsubscribe(string viewerId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(viewerId);

            if (!_subscribers.TryRemove(viewerId, out var queue))
                return false;

            queue.Dispose();
            return true;
        }

        /// <summary>Dequeues the oldest packet queued for <paramref name="viewerId"/>, if any and if it is subscribed.</summary>
        public bool TryDequeue(string viewerId, out VideoFramePacket? packet)
        {
            if (_subscribers.TryGetValue(viewerId, out var queue))
                return queue.TryDequeue(out packet);

            packet = null;
            return false;
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
                _currentSource = source;

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

            foreach (var subscriber in _subscribers.Values)
                subscriber.Enqueue(packet);
        }

        /// <summary>Explicitly stops one generation (cancel, await both loops, dispose once) — used by every lifecycle call that tears one down on purpose.</summary>
        private static async Task StopGenerationAsync(Generation generation)
        {
            generation.Cts.Cancel();

            try { await generation.DecodeTask.ConfigureAwait(false); } catch { /* observed below or by the supervisor */ }
            try { await generation.PublishTask.ConfigureAwait(false); } catch { }

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
            generation.Cts.Dispose();
        }

        /// <summary>One selected video's own source/buffer/pacer/loop-pair. See this type's own remarks on generations.</summary>
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
