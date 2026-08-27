using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Issue #218's own ACs, verified entirely with <see cref="FakeMonotonicClock"/> — "Deterministic
    /// fake-clock tests verify due-time calculations."
    /// </summary>
    public sealed class FramePacerTests
    {
        [Fact]
        public void Constructor_WithNonPositivePlaybackRate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FramePacer(new FakeMonotonicClock(), playbackRate: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FramePacer(new FakeMonotonicClock(), playbackRate: -1));
        }

        [Theory]
        [MemberData(nameof(BeforeStartMembers))]
        public void MembersRequiringStart_BeforeStart_Throw(Action<FramePacer> useMember)
        {
            var pacer = new FramePacer(new FakeMonotonicClock());

            Assert.Throws<InvalidOperationException>(() => useMember(pacer));
        }

        public static IEnumerable<object[]> BeforeStartMembers()
        {
            yield return new object[] { (Action<FramePacer>)(p => p.Pause()) };
            yield return new object[] { (Action<FramePacer>)(p => p.Resume()) };
            yield return new object[] { (Action<FramePacer>)(p => p.ComputeSchedule(TimeSpan.Zero)) };
            yield return new object[] { (Action<FramePacer>)(p => _ = p.CurrentMediaPosition) };
        }

        [Fact]
        public void ComputeSchedule_AtNormalRate_MapsPtsDirectlyToElapsed()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.Zero);

            var schedule = pacer.ComputeSchedule(TimeSpan.FromSeconds(1));

            Assert.Equal(TimeSpan.FromSeconds(1), schedule.DueElapsed);
        }

        [Fact]
        public void ComputeSchedule_UsesStartPtsAsTheZeroPoint()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.FromSeconds(10)); // e.g. resuming mid-stream after a seek

            var schedule = pacer.ComputeSchedule(TimeSpan.FromSeconds(11));

            Assert.Equal(TimeSpan.FromSeconds(1), schedule.DueElapsed);
        }

        [Fact]
        public void ComputeSchedule_AtDoubleRate_HalvesTheElapsedTimeNeeded()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock, playbackRate: 2.0);
            pacer.Start(TimeSpan.Zero);

            var schedule = pacer.ComputeSchedule(TimeSpan.FromSeconds(2));

            Assert.Equal(TimeSpan.FromSeconds(1), schedule.DueElapsed);
        }

        [Fact]
        public void ComputeSchedule_ForIrregularVfrPts_PreservesNonDecreasingOrder()
        {
            // The same deliberately irregular durations #216's own SyntheticVideoFrameSource uses.
            TimeSpan[] durations =
            [
                TimeSpan.FromMilliseconds(33), TimeSpan.FromMilliseconds(41), TimeSpan.FromMilliseconds(33),
                TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(33), TimeSpan.FromMilliseconds(41)
            ];

            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.Zero);

            var pts = TimeSpan.Zero;
            TimeSpan? previousDue = null;

            foreach (var duration in durations)
            {
                var schedule = pacer.ComputeSchedule(pts);

                if (previousDue is not null)
                    Assert.True(schedule.DueElapsed > previousDue.Value);

                previousDue = schedule.DueElapsed;
                pts += duration;
            }
        }

        // #236's own scope, "Cover PTS ordering for CFR/VFR" — the VFR counterpart above already exists
        // (#218's own AC); this proves the same non-decreasing-schedule property holds for a constant
        // frame rate too, since ComputeSchedule makes no CFR/VFR distinction of its own.
        [Fact]
        public void ComputeSchedule_ForConstantFrameRatePts_PreservesNonDecreasingOrder()
        {
            var frameDuration = TimeSpan.FromMilliseconds(1000.0 / 30); // a typical 30fps CFR cadence
            const int frameCount = 10;

            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.Zero);

            var pts = TimeSpan.Zero;
            TimeSpan? previousDue = null;

            for (var i = 0; i < frameCount; i++)
            {
                var schedule = pacer.ComputeSchedule(pts);

                if (previousDue is not null)
                    Assert.True(schedule.DueElapsed > previousDue.Value);

                previousDue = schedule.DueElapsed;
                pts += frameDuration;
            }
        }

        [Fact]
        public void ComputeSchedule_AfterAPriorFrameWasPublishedLate_DoesNotShiftLaterFrames()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.Zero);

            var firstFrameDue = pacer.ComputeSchedule(TimeSpan.FromSeconds(1)).DueElapsed;
            Assert.Equal(TimeSpan.FromSeconds(1), firstFrameDue);

            // Simulate publishing frame 1 half a second late.
            clock.Advance(TimeSpan.FromSeconds(1.5));

            var secondFrameDue = pacer.ComputeSchedule(TimeSpan.FromSeconds(2)).DueElapsed;

            Assert.Equal(TimeSpan.FromSeconds(2), secondFrameDue); // not 2.5s — #218's own AC
        }

        [Fact]
        public void GetPacingError_ReportsLatenessWithoutAffectingComputeSchedule()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.Zero);
            clock.Advance(TimeSpan.FromMilliseconds(1300));

            var error = pacer.GetPacingError(TimeSpan.FromSeconds(1));

            Assert.Equal(TimeSpan.FromMilliseconds(300), error);
            Assert.Equal(TimeSpan.FromSeconds(2), pacer.ComputeSchedule(TimeSpan.FromSeconds(2)).DueElapsed);
        }

        [Fact]
        public void GetPacingError_WhenEarly_IsNegative()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.Zero);
            clock.Advance(TimeSpan.FromMilliseconds(700));

            var error = pacer.GetPacingError(TimeSpan.FromSeconds(1));

            Assert.Equal(TimeSpan.FromMilliseconds(-300), error);
        }

        [Fact]
        public void GetDelayUntilDue_WhenNotYetDue_ReturnsRemainingTime()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.Zero);
            clock.Advance(TimeSpan.FromMilliseconds(400));

            var delay = pacer.GetDelayUntilDue(TimeSpan.FromSeconds(1));

            Assert.Equal(TimeSpan.FromMilliseconds(600), delay);
        }

        [Fact]
        public void GetDelayUntilDue_WhenAlreadyDue_ReturnsZeroNeverNegative()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.Zero);
            clock.Advance(TimeSpan.FromSeconds(5));

            var delay = pacer.GetDelayUntilDue(TimeSpan.FromSeconds(1));

            Assert.Equal(TimeSpan.Zero, delay);
        }

        [Fact]
        public void CurrentMediaPosition_TracksElapsedTimeSinceStart()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.FromSeconds(5));
            clock.Advance(TimeSpan.FromSeconds(2));

            Assert.Equal(TimeSpan.FromSeconds(7), pacer.CurrentMediaPosition);
        }

        [Fact]
        public void Pause_FreezesCurrentMediaPosition_RegardlessOfElapsedRealTime()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.Zero);
            clock.Advance(TimeSpan.FromSeconds(3));

            pacer.Pause();
            clock.Advance(TimeSpan.FromSeconds(7)); // time passes while paused

            Assert.True(pacer.IsPaused);
            Assert.Equal(TimeSpan.FromSeconds(3), pacer.CurrentMediaPosition);
        }

        [Fact]
        public void Pause_IsIdempotent()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.Zero);
            clock.Advance(TimeSpan.FromSeconds(3));

            pacer.Pause();
            clock.Advance(TimeSpan.FromSeconds(10));
            pacer.Pause(); // second call must not move the frozen point

            Assert.Equal(TimeSpan.FromSeconds(3), pacer.CurrentMediaPosition);
        }

        [Fact]
        public void Resume_WhenNotPaused_IsANoOp()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.Zero);
            clock.Advance(TimeSpan.FromSeconds(2));

            pacer.Resume();

            Assert.Equal(TimeSpan.FromSeconds(2), pacer.CurrentMediaPosition);
        }

        [Fact]
        public void PauseThenResume_ShiftsTheEpochByExactlyThePausedDuration_NoDrift()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.Zero);

            clock.Advance(TimeSpan.FromSeconds(2)); // media position reaches 2s
            pacer.Pause();
            clock.Advance(TimeSpan.FromSeconds(3)); // 3s of real time passes while paused
            pacer.Resume();

            // The frame that was due at the original 2s mark should now be due exactly when we resumed
            // (2s original + 3s paused = 5s), matching the clock's current reading precisely.
            var schedule = pacer.ComputeSchedule(TimeSpan.FromSeconds(2));
            Assert.Equal(TimeSpan.FromSeconds(5), schedule.DueElapsed);
            Assert.Equal(TimeSpan.Zero, pacer.GetDelayUntilDue(TimeSpan.FromSeconds(2)));

            // Media position resumes exactly where it left off and continues advancing normally.
            Assert.Equal(TimeSpan.FromSeconds(2), pacer.CurrentMediaPosition);
            clock.Advance(TimeSpan.FromSeconds(1));
            Assert.Equal(TimeSpan.FromSeconds(3), pacer.CurrentMediaPosition);
        }

        [Fact]
        public void ComputeSchedule_WhilePaused_Throws()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock);
            pacer.Start(TimeSpan.Zero);
            pacer.Pause();

            Assert.Throws<InvalidOperationException>(() => pacer.ComputeSchedule(TimeSpan.Zero));
        }

        [Fact]
        public void ComputeSchedule_DerivesDisplayAtUnixTimeMsFromTheSuppliedWallClockAnchor()
        {
            var anchor = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock, wallClockNow: () => anchor);
            pacer.Start(TimeSpan.Zero);

            var schedule = pacer.ComputeSchedule(TimeSpan.FromSeconds(5));

            Assert.Equal(anchor.AddSeconds(5).ToUnixTimeMilliseconds(), schedule.DisplayAtUnixTimeMs);
        }

        [Fact]
        public void Resume_AlsoShiftsTheWallClockAnchorBySamePausedDuration()
        {
            var anchor = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock, wallClockNow: () => anchor);
            pacer.Start(TimeSpan.Zero);

            clock.Advance(TimeSpan.FromSeconds(2));
            pacer.Pause();
            clock.Advance(TimeSpan.FromSeconds(3));
            pacer.Resume();

            var schedule = pacer.ComputeSchedule(TimeSpan.FromSeconds(2));

            Assert.Equal(anchor.AddSeconds(3).AddSeconds(2).ToUnixTimeMilliseconds(), schedule.DisplayAtUnixTimeMs);
        }

        [Fact]
        public void ComputeSchedule_WhenDisplayTimeWouldExceedDateTimeOffsetRange_Throws()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock, wallClockNow: () => DateTimeOffset.MaxValue);
            pacer.Start(TimeSpan.Zero);

            Assert.Throws<ArgumentOutOfRangeException>(() => pacer.ComputeSchedule(TimeSpan.FromDays(1)));
        }

        [Fact]
        public void ComputeSchedule_WhenDisplayTimeWouldPrecedeDateTimeOffsetRange_Throws()
        {
            var clock = new FakeMonotonicClock();
            var pacer = new FramePacer(clock, wallClockNow: () => DateTimeOffset.MinValue);
            pacer.Start(TimeSpan.FromDays(1));

            Assert.Throws<ArgumentOutOfRangeException>(() => pacer.ComputeSchedule(TimeSpan.Zero));
        }
    }
}
