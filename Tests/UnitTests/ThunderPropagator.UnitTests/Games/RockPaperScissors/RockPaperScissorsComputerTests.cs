using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Channels.Games.RockPaperScissors;
using ThunderPropagator.Channels.Games.RockPaperScissors.Channel;
using ThunderPropagator.Channels.Games.RockPaperScissors.Configuration;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.UnitTests.Games.RockPaperScissors
{
    /// <summary>
    /// Issue #12: <see cref="RockPaperScissorsChannelReceiveEvent"/>'s own trigger was <c>async void</c>
    /// and its call site was commented out (dead code, calling a <c>ResponseContext.Subscriptions</c>
    /// member that doesn't exist in this package version), and <see cref="RockPaperScissorsComputer"/>
    /// had at least three further latent bugs once actually exercised — see each test's own remarks.
    ///
    /// Issue #288: <see cref="RockPaperScissorsComputer"/>'s Play/HandleSubscription methods are now
    /// async, going through the persisted <see cref="RockPaperScissorsMatchmakingService"/> instead of
    /// <see cref="RockPaperScissorsChannel"/>'s old in-memory dictionaries — see
    /// RockPaperScissorsMatchmakingServiceTests for that service's own concurrency-safety coverage
    /// (the property this ticket actually exists to fix). Human-vs-human matchmaking
    /// (<see cref="RockPaperScissorsComputer.PlayWithHumanAsync"/>, <see cref="RockPaperScissorsChannel.PeekRandomPlayerAsync"/>'s
    /// own self/already-reserved exclusion) is still not covered here: its candidate pool is
    /// <c>AbstractChannel.Subscriptions</c>, populated only by the framework's own subscribe pipeline,
    /// which this project has no way to drive without a real WebSocket connection — everything
    /// reachable without one is covered directly.
    /// </summary>
    public sealed class RockPaperScissorsComputerTests
    {
        private sealed class FakeRockPaperScissorsContext : IRockPaperScissorsContext
        {
            private readonly List<RockPaperScissorsGameSessionRecord> _sessions = [];
            private readonly HashSet<string> _reservations = [];

            public Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
                => throw new NotSupportedException();

            public Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
            {
                IReadOnlyCollection<TEntity> results = _sessions.OfType<TEntity>().ToList();
                return Task.FromResult(results);
            }

            public Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
            {
                _sessions.Add((RockPaperScissorsGameSessionRecord)(object)entity!);
                return Task.FromResult(entity);
            }

            public Task<bool> TryReserveConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
            {
                lock (_reservations)
                    return Task.FromResult(_reservations.Add(connectionId));
            }
        }

        private static RockPaperScissorsChannel CreateChannel()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IRockPaperScissorsContext>(new FakeRockPaperScissorsContext());
            services.AddScoped<RockPaperScissorsMatchmakingService>();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(new RockPaperScissorsChannelConfiguration());
            services.AddSingleton(Substitute.For<IHostApplicationLifetime>());

            var channel = new RockPaperScissorsChannel(services.BuildServiceProvider());
            channel.Initialize(CancellationToken.None);
            return channel;
        }

        private static int InvokeCompareTo(MoveKind moveKind, MoveKind compareTo)
        {
            var method = typeof(RockPaperScissorsComputer).GetMethod("CompareTo", BindingFlags.NonPublic | BindingFlags.Static)!;
            return (int)method.Invoke(null, [moveKind, compareTo])!;
        }

        // Issue #12's own bug: RockPaperScissorsComputer.Play originally called
        // firstPlayer.Move.CompareTo(secondPlayer.Move) - MoveKind's own built-in enum comparison
        // (ordinal, by underlying int value: Rock=1, Paper=2, Scissor=3) - instead of this private static
        // CompareTo, the method that actually encodes Rock-Paper-Scissors rules. Verified against every
        // one of the nine possible combinations, not just a couple of examples.
        [Theory]
        [InlineData(MoveKind.Rock, MoveKind.Scissor, -1)] // Rock beats Scissor
        [InlineData(MoveKind.Rock, MoveKind.Paper, 1)] // Paper beats Rock
        [InlineData(MoveKind.Rock, MoveKind.Rock, 0)]
        [InlineData(MoveKind.Paper, MoveKind.Rock, -1)] // Paper beats Rock
        [InlineData(MoveKind.Paper, MoveKind.Scissor, 1)] // Scissor beats Paper
        [InlineData(MoveKind.Paper, MoveKind.Paper, 0)]
        [InlineData(MoveKind.Scissor, MoveKind.Paper, -1)] // Scissor beats Paper
        [InlineData(MoveKind.Scissor, MoveKind.Rock, 1)] // Rock beats Scissor
        [InlineData(MoveKind.Scissor, MoveKind.Scissor, 0)]
        public void CompareTo_MatchesRealRockPaperScissorsRules(MoveKind moveKind, MoveKind compareTo, int expected)
        {
            Assert.Equal(expected, InvokeCompareTo(moveKind, compareTo));
        }

        // Issue #12's own bug: Random.Shared.Next(0, array.Length - 1) is exclusive of its own upper
        // bound, so the computer could only ever roll index 0 or 1 (Rock or Paper) out of the three
        // MoveKind values - Scissor (index 2) was unreachable no matter how many times it played.
        [Fact]
        public void Move_CanProduceEveryMoveKindValue()
        {
            var method = typeof(RockPaperScissorsComputer).GetMethod("Move", BindingFlags.NonPublic | BindingFlags.Static)!;
            var seen = new HashSet<MoveKind>();

            for (var i = 0; i < 500 && seen.Count < 3; i++)
                seen.Add((MoveKind)method.Invoke(null, null)!);

            Assert.Equal(3, seen.Count);
        }

        [Fact]
        public async Task PlayWithComputerAsync_DoesNotThrow_AndRecordsASession()
        {
            var channel = CreateChannel();
            var computer = new RockPaperScissorsComputer(channel);
            var player = new Player("Alice", PlayerType.Human, MoveKind.Rock);

            var exception = await Record.ExceptionAsync(() => computer.PlayWithComputerAsync(player));

            Assert.Null(exception);
            var session = Assert.Single(await channel.GetSessionsAsync());
            Assert.Equal("Alice", session.FirstPlayerName);
            Assert.Equal(PlayerType.Computer, session.SecondPlayerType);
        }

        [Fact]
        public async Task PlayWithComputerAsync_CalledTwice_RecordsTwoIndependentSessions()
        {
            var channel = CreateChannel();
            var computer = new RockPaperScissorsComputer(channel);

            await computer.PlayWithComputerAsync(new Player("Alice", PlayerType.Human, MoveKind.Rock));
            await computer.PlayWithComputerAsync(new Player("Bob", PlayerType.Human, MoveKind.Paper));

            Assert.Equal(2, (await channel.GetSessionsAsync()).Count);
        }

        // Issue #12's own scope, "keep a session for the game": HandleSubscriptionAsync is the single
        // entry point RockPaperScissorsChannelReceiveEvent now calls - this proves it's a safe no-op
        // (never throws, never records a session) for a connection that isn't currently subscribed,
        // covering the defensive FindSubscription-returned-null path without needing a real subscription.
        [Fact]
        public async Task HandleSubscriptionAsync_ForAnUnknownConnection_DoesNothing()
        {
            var channel = CreateChannel();
            var computer = new RockPaperScissorsComputer(channel);

            var exception = await Record.ExceptionAsync(() => computer.HandleSubscriptionAsync("unknown-connection"));

            Assert.Null(exception);
            Assert.Empty(await channel.GetSessionsAsync());
        }
    }
}
