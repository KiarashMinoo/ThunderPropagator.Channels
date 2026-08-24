using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// A settable <see cref="IMonotonicClock"/> for deterministic tests — #218's own AC, "Deterministic
    /// fake-clock tests verify due-time calculations."
    /// </summary>
    public sealed class FakeMonotonicClock : IMonotonicClock
    {
        public TimeSpan Elapsed { get; private set; }

        public void Advance(TimeSpan by)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(by, TimeSpan.Zero);

            Elapsed += by;
        }
    }
}
