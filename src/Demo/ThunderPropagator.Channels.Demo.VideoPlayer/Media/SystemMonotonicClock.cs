using System.Diagnostics;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// The production <see cref="IMonotonicClock"/> — backed directly by
    /// <see cref="Stopwatch.GetTimestamp"/>, per #218's own scope. One instance fixes its own reference
    /// point at construction; <see cref="Elapsed"/> is time since then.
    /// </summary>
    public sealed class SystemMonotonicClock : IMonotonicClock
    {
        private readonly long _referenceTimestamp = Stopwatch.GetTimestamp();

        public TimeSpan Elapsed => Stopwatch.GetElapsedTime(_referenceTimestamp);
    }
}
