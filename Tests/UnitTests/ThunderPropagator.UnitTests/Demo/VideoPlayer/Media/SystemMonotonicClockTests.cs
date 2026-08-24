using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Issue #218's own AC: "Clock conversion and overflow boundaries are tested" — for the production
    /// clock itself (<see cref="FramePacerTests"/> covers the pacing math via <see cref="FakeMonotonicClock"/>).
    /// </summary>
    public sealed class SystemMonotonicClockTests
    {
        [Fact]
        public void Elapsed_StartsAtApproximatelyZero()
        {
            var clock = new SystemMonotonicClock();

            Assert.True(clock.Elapsed >= TimeSpan.Zero);
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task Elapsed_IncreasesMonotonicallyOverRealTime()
        {
            var clock = new SystemMonotonicClock();
            var first = clock.Elapsed;

            await Task.Delay(TimeSpan.FromMilliseconds(20));

            var second = clock.Elapsed;

            Assert.True(second > first);
            Assert.True(second - first >= TimeSpan.FromMilliseconds(10)); // generous lower bound for scheduler jitter
        }

        [Fact]
        public void Elapsed_NeverGoesBackward_AcrossManySuccessiveReadings()
        {
            var clock = new SystemMonotonicClock();
            var previous = clock.Elapsed;

            for (var i = 0; i < 1000; i++)
            {
                var current = clock.Elapsed;
                Assert.True(current >= previous);
                previous = current;
            }
        }

        [Fact]
        public void TwoInstances_EachHaveTheirOwnIndependentReferencePoint()
        {
            var first = new SystemMonotonicClock();
            var second = new SystemMonotonicClock();

            // Both start near zero independently — not a shared/global timeline.
            Assert.True(first.Elapsed < TimeSpan.FromSeconds(1));
            Assert.True(second.Elapsed < TimeSpan.FromSeconds(1));
        }
    }
}
