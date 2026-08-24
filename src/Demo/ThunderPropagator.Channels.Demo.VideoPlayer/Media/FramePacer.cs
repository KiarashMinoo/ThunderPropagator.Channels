namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Computes, for a session's currently selected video, exactly when each decoded frame should be
    /// published — from source PTS against an <see cref="IMonotonicClock"/>, never from repeated
    /// wall-clock delays or a fixed-FPS assumption, so fixed- and variable-frame-rate media both play
    /// back at the correct pace. #218's own scope in full.
    /// </summary>
    /// <remarks>
    /// <b>Why drift never accumulates:</b> every <see cref="ComputeSchedule"/> call derives its answer
    /// from the same fixed epoch (<see cref="Start"/>'s own moment, shifted only by <see cref="Pause"/>/
    /// <see cref="Resume"/> — see below) and that frame's own <c>framePts</c> — never from the actual
    /// publish time of whichever frame came before it. A frame published late (because encoding or the
    /// network momentarily fell behind) never shifts the epoch, so every later frame's due time is
    /// exactly as if the late one had never happened — #218's own AC, "Normal scheduling jitter does not
    /// permanently shift later frames." <see cref="GetPacingError"/> reports how late/early a frame
    /// actually was purely for diagnostics; it never feeds back into the schedule.
    /// <para/>
    /// <b>Pause/resume:</b> <see cref="Pause"/> freezes the timeline at the instant it's called;
    /// <see cref="Resume"/> shifts the epoch forward by exactly however long playback was paused, so
    /// every PTS's own due time (and <see cref="CurrentMediaPosition"/>) is unaffected by how long the
    /// pause itself lasted — #218's own AC, "Pause/resume preserves media position and timeline
    /// continuity."
    /// </remarks>
    public sealed class FramePacer
    {
        private readonly IMonotonicClock _clock;
        private readonly double _playbackRate;
        private readonly Func<DateTimeOffset> _wallClockNow;

        private bool _started;
        private TimeSpan _startPts;
        private TimeSpan _epochElapsed;
        private DateTimeOffset _epochWallClock;
        private TimeSpan? _pausedAtElapsed;

        /// <param name="clock">The monotonic clock every schedule is computed against.</param>
        /// <param name="playbackRate">Speed multiplier — 1.0 for normal speed. Must be strictly positive.</param>
        /// <param name="wallClockNow">
        /// Supplies the wall-clock instant used to anchor <see cref="FrameSchedule.DisplayAtUnixTimeMs"/>.
        /// Defaults to <see cref="DateTimeOffset.UtcNow"/>; overridable so tests can assert an exact
        /// resulting value deterministically, matching this same ticket's own "an injectable monotonic
        /// clock abstraction" for the elapsed-time side.
        /// </param>
        public FramePacer(IMonotonicClock clock, double playbackRate = 1.0, Func<DateTimeOffset>? wallClockNow = null)
        {
            ArgumentNullException.ThrowIfNull(clock);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(playbackRate, 0.0);

            _clock = clock;
            _playbackRate = playbackRate;
            _wallClockNow = wallClockNow ?? (() => DateTimeOffset.UtcNow);
        }

        /// <summary>Whether <see cref="Pause"/> has been called without a matching <see cref="Resume"/> since.</summary>
        public bool IsPaused => _pausedAtElapsed is not null;

        /// <summary>
        /// The current playback position — the <c>startPts</c> passed to the most recent
        /// <see cref="Start"/>, plus elapsed monotonic time scaled by the playback rate. Frozen at
        /// whatever it was the instant <see cref="Pause"/> was called, for as long as <see cref="IsPaused"/>.
        /// </summary>
        public TimeSpan CurrentMediaPosition
        {
            get
            {
                EnsureStarted();

                var elapsedSinceEpoch = (_pausedAtElapsed ?? _clock.Elapsed) - _epochElapsed;
                return _startPts + elapsedSinceEpoch * _playbackRate;
            }
        }

        /// <summary>
        /// (Re)starts the timeline: <paramref name="startPts"/> is the media position "now" corresponds
        /// to from this point on — zero for playback from the beginning, or a seek target. Clears any
        /// prior <see cref="Pause"/> state. Safe to call again (e.g. after a seek) — a fresh
        /// <see cref="Start"/> call establishes a brand-new epoch outright, superseding whatever
        /// schedule was in effect before.
        /// </summary>
        public void Start(TimeSpan startPts)
        {
            _startPts = startPts;
            _epochElapsed = _clock.Elapsed;
            _epochWallClock = _wallClockNow();
            _pausedAtElapsed = null;
            _started = true;
        }

        /// <summary>Freezes the timeline. Idempotent — a second call while already paused is a no-op.</summary>
        public void Pause()
        {
            EnsureStarted();

            _pausedAtElapsed ??= _clock.Elapsed;
        }

        /// <summary>Resumes a paused timeline, shifting the epoch forward by exactly how long it was paused. Idempotent — a call while not paused is a no-op.</summary>
        public void Resume()
        {
            EnsureStarted();

            if (_pausedAtElapsed is not { } pausedAtElapsed)
                return;

            var pausedDuration = _clock.Elapsed - pausedAtElapsed;
            _epochElapsed += pausedDuration;
            _epochWallClock += pausedDuration;
            _pausedAtElapsed = null;
        }

        /// <summary>Computes when the frame at <paramref name="framePts"/> should be published.</summary>
        /// <exception cref="InvalidOperationException"><see cref="Start"/> has not been called, or the timeline is currently paused.</exception>
        public FrameSchedule ComputeSchedule(TimeSpan framePts)
        {
            EnsureStarted();

            if (IsPaused)
                throw new InvalidOperationException($"Cannot compute a schedule while paused — call {nameof(Resume)} first.");

            var offsetSincePts = (framePts - _startPts) / _playbackRate;
            var dueElapsed = _epochElapsed + offsetSincePts;
            var displayAt = _epochWallClock + offsetSincePts;

            return new FrameSchedule(dueElapsed, displayAt.ToUnixTimeMilliseconds());
        }

        /// <summary>
        /// How far off the current moment already is from <paramref name="framePts"/>'s own due time —
        /// positive when late, negative when early. Diagnostic only: this value is never fed back into
        /// <see cref="ComputeSchedule"/> for this or any other frame — see this type's own remarks.
        /// </summary>
        public TimeSpan GetPacingError(TimeSpan framePts) => _clock.Elapsed - ComputeSchedule(framePts).DueElapsed;

        /// <summary>How long to wait before <paramref name="framePts"/> is due — zero (never negative) if it is already due.</summary>
        public TimeSpan GetDelayUntilDue(TimeSpan framePts)
        {
            var remaining = ComputeSchedule(framePts).DueElapsed - _clock.Elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        private void EnsureStarted()
        {
            if (!_started)
                throw new InvalidOperationException($"{nameof(Start)} must be called before using this {nameof(FramePacer)}.");
        }
    }
}
