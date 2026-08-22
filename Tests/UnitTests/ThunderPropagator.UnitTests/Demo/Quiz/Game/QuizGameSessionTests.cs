using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;
using ThunderPropagator.Channels.Demo.Quiz.Game.Exceptions;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Game
{
    /// <summary>
    /// Issue #187: covers QuizGameSession's membership rules — the first joiner becomes host and every
    /// later join leaves host unchanged, a name already connected is rejected as a duplicate join, a
    /// name whose connection already disconnected is instead reconnected preserving its identity, and
    /// disconnect/abandonment tracking. <see cref="QuizPlayerJoinResult.CurrentState"/>'s snapshot
    /// behavior is covered directly against <see cref="QuizGameSession.UpdateCurrentState"/>.
    /// </summary>
    public sealed class QuizGameSessionTests
    {
        private const string GameId = "game-1";

        [Fact]
        public void Constructor_WithNullOrWhiteSpaceGameId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new QuizGameSession(" "));
        }

        [Fact]
        public void Join_FirstPlayer_BecomesHostAndIsNotAReconnect()
        {
            var session = new QuizGameSession(GameId);

            var result = session.Join("Alice", "conn-1");

            Assert.False(result.IsReconnect);
            Assert.True(result.Player.IsHost);
            Assert.Equal("Alice", session.HostPlayerName);
        }

        [Fact]
        public void Join_SecondPlayer_IsNotHost()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");

            var result = session.Join("Bob", "conn-2");

            Assert.False(result.Player.IsHost);
            Assert.Equal("Alice", session.HostPlayerName);
        }

        [Fact]
        public void Join_SameNameWhileStillConnected_ThrowsDuplicateJoin()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");

            var exception = Assert.Throws<QuizDuplicateJoinException>(() => session.Join("Alice", "conn-2"));

            Assert.Equal(GameId, exception.GameId);
            Assert.Equal("Alice", exception.PlayerName);
        }

        [Fact]
        public void Join_SameNameWhileStillConnected_LeavesExistingConnectionUnchanged()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");

            Assert.Throws<QuizDuplicateJoinException>(() => session.Join("Alice", "conn-2"));

            var player = Assert.Single(session.Players);
            Assert.Equal("conn-1", player.ConnectionId);
            Assert.True(player.IsConnected);
        }

        [Fact]
        public void Join_SameNameAfterDisconnect_ReconnectsRatherThanDuplicating()
        {
            var session = new QuizGameSession(GameId);
            var originalJoin = session.Join("Alice", "conn-1");
            session.Disconnect("conn-1");

            var result = session.Join("Alice", "conn-2");

            Assert.True(result.IsReconnect);
            Assert.Same(originalJoin.Player, result.Player);
            Assert.Equal("conn-2", result.Player.ConnectionId);
            Assert.True(result.Player.IsConnected);
            Assert.Single(session.Players);
        }

        [Fact]
        public void Join_Reconnect_PreservesHostIdentity()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");
            session.Disconnect("conn-1");

            var result = session.Join("Alice", "conn-2");

            Assert.True(result.Player.IsHost);
            Assert.Equal("Alice", session.HostPlayerName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Join_WithNullOrWhiteSpacePlayerName_Throws(string? playerName)
        {
            var session = new QuizGameSession(GameId);

            Assert.ThrowsAny<ArgumentException>(() => session.Join(playerName!, "conn-1"));
        }

        [Fact]
        public void Disconnect_UnknownConnectionId_ReturnsFalseAndDoesNotThrow()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");

            var disconnected = session.Disconnect("conn-does-not-exist");

            Assert.False(disconnected);
        }

        [Fact]
        public void Disconnect_KnownConnection_ReturnsTrueAndMarksPlayerDisconnected()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");

            var disconnected = session.Disconnect("conn-1");

            Assert.True(disconnected);
            var player = Assert.Single(session.Players);
            Assert.False(player.IsConnected);
        }

        [Fact]
        public void Disconnect_AlreadyDisconnectedConnection_ReturnsFalse()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");
            session.Disconnect("conn-1");

            var disconnectedAgain = session.Disconnect("conn-1");

            Assert.False(disconnectedAgain);
        }

        [Fact]
        public void IsAbandoned_WhenNobodyHasEverJoined_IsTrue()
        {
            var session = new QuizGameSession(GameId);

            Assert.True(session.IsAbandoned);
        }

        [Fact]
        public void IsAbandoned_WithAConnectedPlayer_IsFalse()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");

            Assert.False(session.IsAbandoned);
        }

        [Fact]
        public void IsAbandoned_AfterEveryPlayerDisconnects_IsTrue()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");
            session.Join("Bob", "conn-2");
            session.Disconnect("conn-1");
            session.Disconnect("conn-2");

            Assert.True(session.IsAbandoned);
        }

        [Fact]
        public void IsAbandoned_WithOneOfTwoPlayersStillConnected_IsFalse()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");
            session.Join("Bob", "conn-2");
            session.Disconnect("conn-1");

            Assert.False(session.IsAbandoned);
        }

        [Fact]
        public void CurrentState_BeforeAnyUpdate_IsNull()
        {
            var session = new QuizGameSession(GameId);

            Assert.Null(session.CurrentState);
        }

        [Fact]
        public void Join_BeforeAnyStateRecorded_ReturnsNullSnapshot()
        {
            var session = new QuizGameSession(GameId);

            var result = session.Join("Alice", "conn-1");

            Assert.Null(result.CurrentState);
        }

        [Fact]
        public void Join_AfterStateRecorded_ReturnsThatSnapshotOnly()
        {
            var session = new QuizGameSession(GameId);
            var state = new QuizChannelFeederMessage { GameId = GameId, Phase = QuizPhase.Question, QuestionText = "2+2?" };
            session.UpdateCurrentState(state);

            var result = session.Join("Alice", "conn-1");

            Assert.Same(state, result.CurrentState);
        }

        [Fact]
        public void UpdateCurrentState_Supersedes_EarlierState()
        {
            var session = new QuizGameSession(GameId);
            session.UpdateCurrentState(new QuizChannelFeederMessage { GameId = GameId, Phase = QuizPhase.Lobby });
            var latest = new QuizChannelFeederMessage { GameId = GameId, Phase = QuizPhase.Scoreboard };

            session.UpdateCurrentState(latest);

            Assert.Same(latest, session.CurrentState);
        }

        [Fact]
        public void UpdateCurrentState_WithNull_Throws()
        {
            var session = new QuizGameSession(GameId);

            Assert.Throws<ArgumentNullException>(() => session.UpdateCurrentState(null!));
        }

        // Issue #187's own AC: "Concurrency tests cover join/disconnect/reconnect during phase
        // transitions." Every thread races a join-then-disconnect-then-rejoin cycle for its own
        // distinct player name while phase transitions happen concurrently on the same session's
        // PhaseStateMachine — membership and phase are independently locked (see QuizGameSession's own
        // remarks), so neither should corrupt or block the other, and every player should end up
        // connected exactly once.
        [Fact]
        public void JoinDisconnectReconnect_ConcurrentlyAcrossPlayers_WhilePhaseTransitionsRun_NeverCorruptsState()
        {
            const int playerCount = 16;
            var session = new QuizGameSession(GameId);
            using var barrier = new Barrier(playerCount + 1);

            var phaseDriver = new Thread(() =>
            {
                barrier.SignalAndWait();
                session.PhaseStateMachine.StartGame();
                session.PhaseStateMachine.RevealAnswer();
                session.PhaseStateMachine.ShowScoreboard();
            });

            var playerThreads = Enumerable.Range(0, playerCount)
                .Select(index => new Thread(() =>
                {
                    var playerName = $"Player{index}";
                    barrier.SignalAndWait();

                    var firstJoin = session.Join(playerName, $"conn-{index}-a");
                    session.Disconnect(firstJoin.Player.ConnectionId);
                    session.Join(playerName, $"conn-{index}-b");
                }))
                .ToArray();

            phaseDriver.Start();
            foreach (var thread in playerThreads)
                thread.Start();

            phaseDriver.Join();
            foreach (var thread in playerThreads)
                thread.Join();

            Assert.Equal(playerCount, session.Players.Count);
            Assert.All(session.Players, player => Assert.True(player.IsConnected));
            Assert.All(session.Players, player => Assert.Equal('b', player.ConnectionId[^1]));
            Assert.Equal(QuizPhase.Scoreboard, session.PhaseStateMachine.CurrentPhase);
        }
    }
}
