using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Channels.Games.RockPaperScissors;
using ThunderPropagator.Channels.Games.RockPaperScissors.Channel;
using ThunderPropagator.Channels.Games.RockPaperScissors.Configuration;

namespace ThunderPropagator.UnitTests.Games.RockPaperScissors
{
    /// <summary>
    /// Issue #12: <see cref="RockPaperScissorsChannelReceiveEvent"/>'s own trigger was <c>async void</c>
    /// and its call site was commented out (dead code, calling a <c>ResponseContext.Subscriptions</c>
    /// member that doesn't exist in this package version), and <see cref="RockPaperScissorsComputer"/>
    /// had at least three further latent bugs once actually exercised — see each test's own remarks.
    /// Human-vs-human matchmaking (<see cref="RockPaperScissorsComputer.PlayWithHuman"/>,
    /// <see cref="RockPaperScissorsChannel.PeekRandomPlayer"/>'s own self/already-played exclusion) is
    /// not covered here: constructing a real <c>Subscription</c> requires the framework's own subscribe
    /// pipeline (its constructor is internal to a separate package assembly this project has no
    /// <c>InternalsVisibleTo</c> into), and this module — unlike TicTacToe — has no existing subscribe
    /// entry point or working reference to build one against safely. Everything reachable without a real
    /// subscription is covered directly.
    /// </summary>
    public sealed class RockPaperScissorsComputerTests
    {
        private static RockPaperScissorsChannel CreateChannel()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(RockPaperScissorsChannelConfiguration)).Returns(new RockPaperScissorsChannelConfiguration());

            var channel = new RockPaperScissorsChannel(serviceProvider);
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
        public void PlayWithComputer_DoesNotThrow_AndRecordsASession()
        {
            var channel = CreateChannel();
            var computer = new RockPaperScissorsComputer(channel);
            var player = new Player("Alice", PlayerType.Human, MoveKind.Rock);

            var exception = Record.Exception(() => computer.PlayWithComputer(player));

            Assert.Null(exception);
            var session = Assert.Single(channel.GetSessions());
            Assert.Equal("Alice", session.FirstPlayer.Name);
            Assert.Equal(PlayerType.Computer, session.SecondPlayer.PlayerType);
        }

        [Fact]
        public void PlayWithComputer_CalledTwice_RecordsTwoIndependentSessions()
        {
            var channel = CreateChannel();
            var computer = new RockPaperScissorsComputer(channel);

            computer.PlayWithComputer(new Player("Alice", PlayerType.Human, MoveKind.Rock));
            computer.PlayWithComputer(new Player("Bob", PlayerType.Human, MoveKind.Paper));

            Assert.Equal(2, channel.GetSessions().Count);
        }

        // Issue #12's own scope, "keep a session for the game": HandleSubscription is the single entry
        // point RockPaperScissorsChannelReceiveEvent now calls - this proves it's a safe no-op (never
        // throws, never records a session) for a connection that isn't currently subscribed, covering the
        // defensive FindSubscription-returned-null path without needing a real subscription.
        [Fact]
        public void HandleSubscription_ForAnUnknownConnection_DoesNothing()
        {
            var channel = CreateChannel();
            var computer = new RockPaperScissorsComputer(channel);

            var exception = Record.Exception(() => computer.HandleSubscription("unknown-connection"));

            Assert.Null(exception);
            Assert.Empty(channel.GetSessions());
        }
    }
}
