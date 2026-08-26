using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.UnitTests.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Session
{
    /// <summary>
    /// Issue #229's own ACs: authorized submissions succeed and aggregate, disabled/invalid/rate-limited
    /// input is rejected, expired reactions disappear within the documented tolerance, and all of it is
    /// safe under concurrent submissions from many viewers.
    /// </summary>
    public sealed class ReactionAggregatorTests
    {
        private static ReactionAggregator CreateAggregator(
            FakeMonotonicClock clock,
            IReadOnlySet<string>? allowedReactions = null,
            TimeSpan? reactionWindow = null,
            int maxReactionsPerViewerPerWindow = 3,
            Action<string, string, ReactionRejectionReason>? onRejected = null) =>
            new(
                clock,
                allowedReactions ?? new HashSet<string> { "like", "love" },
                reactionWindow ?? TimeSpan.FromSeconds(10),
                maxReactionsPerViewerPerWindow,
                onRejected);

        [Fact]
        public void TryRecord_AllowedReaction_UnderRateLimit_Succeeds_AndAppearsInSnapshot()
        {
            var clock = new FakeMonotonicClock();
            var aggregator = CreateAggregator(clock);

            var recorded = aggregator.TryRecord("viewerA", "like", out var reason);

            Assert.True(recorded);
            Assert.Equal(default, reason);
            var snapshot = aggregator.GetSnapshot();
            Assert.Single(snapshot);
            Assert.Equal("like", snapshot[0].Reaction);
            Assert.Equal(1, snapshot[0].Count);
        }

        [Fact]
        public void TryRecord_MultipleViewers_SameReaction_AggregatesCount()
        {
            var clock = new FakeMonotonicClock();
            var aggregator = CreateAggregator(clock);

            aggregator.TryRecord("viewerA", "like", out _);
            aggregator.TryRecord("viewerB", "like", out _);
            aggregator.TryRecord("viewerA", "love", out _);

            var snapshot = aggregator.GetSnapshot();
            Assert.Equal(2, snapshot.Single(r => r.Reaction == "like").Count);
            Assert.Equal(1, snapshot.Single(r => r.Reaction == "love").Count);
        }

        [Fact]
        public void TryRecord_UnknownReaction_IsRejected_WithUnknownReason()
        {
            var clock = new FakeMonotonicClock();
            var aggregator = CreateAggregator(clock);

            var recorded = aggregator.TryRecord("viewerA", "not-a-real-reaction", out var reason);

            Assert.False(recorded);
            Assert.Equal(ReactionRejectionReason.Unknown, reason);
            Assert.Empty(aggregator.GetSnapshot());
        }

        [Fact]
        public void TryRecord_ReactionExceedingMaxLength_IsRejected_WithTooLongReason()
        {
            var clock = new FakeMonotonicClock();
            var tooLong = new string('a', 33); // ReactionNameMaxLength is 32
            var aggregator = CreateAggregator(clock, allowedReactions: new HashSet<string> { tooLong });

            var recorded = aggregator.TryRecord("viewerA", tooLong, out var reason);

            Assert.False(recorded);
            Assert.Equal(ReactionRejectionReason.TooLong, reason);
        }

        [Fact]
        public void TryRecord_ExceedingPerViewerRateLimit_IsRejected_WithRateLimitedReason()
        {
            var clock = new FakeMonotonicClock();
            var aggregator = CreateAggregator(clock, maxReactionsPerViewerPerWindow: 2);

            Assert.True(aggregator.TryRecord("viewerA", "like", out _));
            Assert.True(aggregator.TryRecord("viewerA", "like", out _));
            var recorded = aggregator.TryRecord("viewerA", "like", out var reason);

            Assert.False(recorded);
            Assert.Equal(ReactionRejectionReason.RateLimited, reason);
            Assert.Equal(2, aggregator.GetSnapshot().Single().Count); // the rejected attempt must not have been counted
        }

        [Fact]
        public void TryRecord_RateLimitedViewer_CanReactAgain_AfterWindowElapses()
        {
            var clock = new FakeMonotonicClock();
            var window = TimeSpan.FromSeconds(5);
            var aggregator = CreateAggregator(clock, reactionWindow: window, maxReactionsPerViewerPerWindow: 1);

            Assert.True(aggregator.TryRecord("viewerA", "like", out _));
            Assert.False(aggregator.TryRecord("viewerA", "like", out var reason));
            Assert.Equal(ReactionRejectionReason.RateLimited, reason);

            clock.Advance(window + TimeSpan.FromMilliseconds(1));

            Assert.True(aggregator.TryRecord("viewerA", "like", out _));
        }

        [Fact]
        public void TryRecord_RateLimit_IsPerViewer_NotGlobal()
        {
            var clock = new FakeMonotonicClock();
            var aggregator = CreateAggregator(clock, maxReactionsPerViewerPerWindow: 1);

            Assert.True(aggregator.TryRecord("viewerA", "like", out _));
            Assert.True(aggregator.TryRecord("viewerB", "like", out _)); // a different viewer's own limit is independent
        }

        [Fact]
        public void GetSnapshot_ReactionOlderThanWindow_IsExpired_AndOmitted()
        {
            var clock = new FakeMonotonicClock();
            var window = TimeSpan.FromSeconds(10);
            var aggregator = CreateAggregator(clock, reactionWindow: window);

            aggregator.TryRecord("viewerA", "like", out _);
            Assert.Single(aggregator.GetSnapshot());

            clock.Advance(window + TimeSpan.FromMilliseconds(1));

            Assert.Empty(aggregator.GetSnapshot());
        }

        [Fact]
        public void GetSnapshot_SomeExpiredSomeNot_OnlyReportsStillLiveOnes()
        {
            var clock = new FakeMonotonicClock();
            var window = TimeSpan.FromSeconds(10);
            var aggregator = CreateAggregator(clock, reactionWindow: window);

            aggregator.TryRecord("viewerA", "like", out _);
            clock.Advance(TimeSpan.FromSeconds(6));
            aggregator.TryRecord("viewerB", "love", out _);
            clock.Advance(TimeSpan.FromSeconds(5)); // "like" (age 11s) now expired, "love" (age 5s) still live

            var snapshot = aggregator.GetSnapshot();
            Assert.Single(snapshot);
            Assert.Equal("love", snapshot[0].Reaction);
        }

        [Fact]
        public void OnRejected_Fires_WithTheRejectingViewerReactionAndReason()
        {
            var clock = new FakeMonotonicClock();
            var calls = new List<(string ViewerId, string Reaction, ReactionRejectionReason Reason)>();
            var aggregator = CreateAggregator(clock, onRejected: (viewerId, reaction, reason) => calls.Add((viewerId, reaction, reason)));

            aggregator.TryRecord("viewerA", "not-allowed", out _);

            var call = Assert.Single(calls);
            Assert.Equal("viewerA", call.ViewerId);
            Assert.Equal("not-allowed", call.Reaction);
            Assert.Equal(ReactionRejectionReason.Unknown, call.Reason);
        }

        [Fact]
        public void OnRejected_DoesNotFire_OnASuccessfulRecord()
        {
            var clock = new FakeMonotonicClock();
            var fired = false;
            var aggregator = CreateAggregator(clock, onRejected: (_, _, _) => fired = true);

            aggregator.TryRecord("viewerA", "like", out _);

            Assert.False(fired);
        }

        [Fact]
        public async Task TryRecord_ConcurrentSubmissions_FromManyViewers_ProducesCorrectAggregateCount()
        {
            // Plain concurrent Task.Run calls, no Barrier rendezvous — a barrier here would only add an
            // artificial rendezvous point without proving anything a natural race doesn't already prove,
            // and this module has an established pitfall where Barrier-based tests starve the thread pool
            // and cascade failures into unrelated tests.
            var clock = new FakeMonotonicClock();
            var aggregator = CreateAggregator(clock, maxReactionsPerViewerPerWindow: 1000);

            const int viewerCount = 50;
            var tasks = Enumerable.Range(0, viewerCount)
                .Select(i => Task.Run(() => aggregator.TryRecord($"viewer-{i}", "like", out _)))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            Assert.All(results, Assert.True);
            Assert.Equal(viewerCount, aggregator.GetSnapshot().Single().Count);
        }

        [Fact]
        public void Constructor_WithInvalidArguments_Throws()
        {
            var clock = new FakeMonotonicClock();
            var allowed = new HashSet<string> { "like" };

            Assert.Throws<ArgumentNullException>(() => new ReactionAggregator(null!, allowed, TimeSpan.FromSeconds(1), 1));
            Assert.Throws<ArgumentNullException>(() => new ReactionAggregator(clock, null!, TimeSpan.FromSeconds(1), 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReactionAggregator(clock, allowed, TimeSpan.Zero, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReactionAggregator(clock, allowed, TimeSpan.FromSeconds(1), 0));
        }
    }
}
