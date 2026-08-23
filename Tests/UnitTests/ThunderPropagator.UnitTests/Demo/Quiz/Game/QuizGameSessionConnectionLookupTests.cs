using ThunderPropagator.Channels.Demo.Quiz.Game;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Game
{
    /// <summary>
    /// Issue #192: covers QuizGameSession.TryGetPlayerByConnectionId — the server-side identity
    /// resolution QuizChannel.SubmitAnswer uses instead of trusting a caller-supplied player name.
    /// </summary>
    public sealed class QuizGameSessionConnectionLookupTests
    {
        private const string GameId = "game-1";

        [Fact]
        public void TryGetPlayerByConnectionId_ForAnUnknownConnection_ReturnsNull()
        {
            var session = new QuizGameSession(GameId);

            Assert.Null(session.TryGetPlayerByConnectionId("does-not-exist"));
        }

        [Fact]
        public void TryGetPlayerByConnectionId_ForAJoinedConnection_ReturnsThatPlayer()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");

            var player = session.TryGetPlayerByConnectionId("conn-1");

            Assert.NotNull(player);
            Assert.Equal("Alice", player.PlayerName);
        }

        [Fact]
        public void TryGetPlayerByConnectionId_AfterDisconnect_ReturnsNullForTheOldConnection()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");
            session.Disconnect("conn-1");

            Assert.Null(session.TryGetPlayerByConnectionId("conn-1"));
        }

        [Fact]
        public void TryGetPlayerByConnectionId_AfterReconnectUnderADifferentConnection_ResolvesTheNewConnection()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");
            session.Disconnect("conn-1");
            session.Join("Alice", "conn-2");

            var player = session.TryGetPlayerByConnectionId("conn-2");

            Assert.NotNull(player);
            Assert.Equal("Alice", player.PlayerName);
        }

        [Fact]
        public void TryGetPlayerByConnectionId_AfterReconnectUnderADifferentConnection_NoLongerResolvesTheOldConnection()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");
            session.Disconnect("conn-1");
            session.Join("Alice", "conn-2");

            Assert.Null(session.TryGetPlayerByConnectionId("conn-1"));
        }

        [Fact]
        public void TryGetPlayerByConnectionId_DoesNotConfuseTwoDifferentPlayersInTheSameSession()
        {
            var session = new QuizGameSession(GameId);
            session.Join("Alice", "conn-1");
            session.Join("Bob", "conn-2");

            Assert.Equal("Alice", session.TryGetPlayerByConnectionId("conn-1")!.PlayerName);
            Assert.Equal("Bob", session.TryGetPlayerByConnectionId("conn-2")!.PlayerName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void TryGetPlayerByConnectionId_WithNullOrWhiteSpaceConnectionId_Throws(string? connectionId)
        {
            var session = new QuizGameSession(GameId);

            Assert.ThrowsAny<ArgumentException>(() => session.TryGetPlayerByConnectionId(connectionId!));
        }
    }
}
