namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// A source of monotonically non-decreasing elapsed time, immune to system wall-clock adjustments
    /// (NTP sync, timezone/DST changes, a user changing the system clock) — #218's own scope: "an
    /// injectable monotonic clock abstraction." <see cref="FramePacer"/> schedules every frame from this
    /// clock alone; wall-clock time only enters at the very end, when converting a due time to
    /// <see cref="FrameSchedule.DisplayAtUnixTimeMs"/> for client consumption.
    /// </summary>
    /// <remarks>
    /// <see cref="Elapsed"/> is only meaningful as a difference between two readings of the <i>same</i>
    /// clock instance — it measures time since an arbitrary reference point fixed when that instance was
    /// created, never a calendar date/time. This mirrors <see cref="System.Diagnostics.Stopwatch"/>'s
    /// own contract, which <see cref="SystemMonotonicClock"/> is backed by.
    /// </remarks>
    public interface IMonotonicClock
    {
        /// <summary>Elapsed time since this clock instance's own fixed reference point.</summary>
        TimeSpan Elapsed { get; }
    }
}
