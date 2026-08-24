namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// When one frame is due to be published, as computed by <see cref="FramePacer.ComputeSchedule"/>.
    /// </summary>
    /// <param name="DueElapsed">
    /// The <see cref="IMonotonicClock.Elapsed"/> value, on the same clock instance the owning
    /// <see cref="FramePacer"/> was constructed with, at which this frame should be published. Compare
    /// directly against that clock's current <see cref="IMonotonicClock.Elapsed"/> to know how long to
    /// wait (or how late this frame already is).
    /// </param>
    /// <param name="DisplayAtUnixTimeMs">
    /// <paramref name="DueElapsed"/> converted to an absolute Unix-epoch-milliseconds instant via the
    /// <see cref="FramePacer"/>'s own synchronized wall-clock/monotonic anchor — #218's own scope,
    /// "Derive DisplayAtUnixTimeMs from one synchronized wall-clock/monotonic anchor." Suitable to send
    /// to a client, which has no access to the server's own monotonic clock.
    /// </param>
    public readonly record struct FrameSchedule(TimeSpan DueElapsed, long DisplayAtUnixTimeMs);
}
