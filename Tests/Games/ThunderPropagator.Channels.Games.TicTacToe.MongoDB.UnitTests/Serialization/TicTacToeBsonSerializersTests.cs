using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;
using ThunderPropagator.Channels.Games.TicTacToe.Models;
using ThunderPropagator.Channels.Games.TicTacToe.MongoDB.Serialization;

namespace ThunderPropagator.UnitTests.Games.TicTacToe.MongoDB.Serialization
{
    /// <summary>
    /// TicTacToeGameRecord has a private constructor and get-only properties, so it has a hand-written
    /// serializer — mirrors ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB's own
    /// RockPaperScissorsBsonSerializersTests, extended to cover both the waiting-for-opponent state
    /// (Player2* all null) and the started state (Player2* populated), since TicTacToeGameRecord —
    /// unlike RockPaperScissors' write-once record — is mutated across its lifetime.
    /// These round-trip real instances through ToBsonDocument()/BsonSerializer.Deserialize — pure
    /// in-memory BSON (de)serialization, no MongoDB server needed.
    /// </summary>
    public sealed class TicTacToeBsonSerializersTests
    {
        public TicTacToeBsonSerializersTests() => TicTacToeBsonSerializers.EnsureRegistered();

        private static TicTacToeGameRecord CreateWaitingRecord(string sessionId = "session-1")
            => TicTacToeGameRecord.CreateWaitingForOpponent(sessionId, "---------", "Alice", PlayerSign.X, "connection-1");

        [Fact]
        public void WaitingForOpponentRecord_RoundTripsThroughBson()
        {
            var record = CreateWaitingRecord();

            var document = record.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<TicTacToeGameRecord>(document);

            Assert.Equal(record.SessionId, deserialized.SessionId);
            Assert.Equal(record.Board, deserialized.Board);
            Assert.Equal(record.Player1Name, deserialized.Player1Name);
            Assert.Equal(record.Player1Sign, deserialized.Player1Sign);
            Assert.Equal(record.Player1ConnectionId, deserialized.Player1ConnectionId);
            Assert.Null(deserialized.Player2Name);
            Assert.Null(deserialized.Player2Kind);
            Assert.Null(deserialized.Player2ConnectionId);
            Assert.Null(deserialized.Player2DifficultyLevel);
            Assert.Null(deserialized.CurrentTurnSign);
        }

        [Fact]
        public void SessionIdIsStoredUnderTheMongoIdField()
        {
            var record = CreateWaitingRecord();

            var document = record.ToBsonDocument();

            Assert.True(document.Contains("_id"));
            Assert.Equal("session-1", document["_id"].AsString);
        }

        [Fact]
        public void StartedGameAgainstAHumanOpponent_RoundTripsThroughBson()
        {
            var record = CreateWaitingRecord();
            record.Start("X--------", "Bob", PlayerKind.Human, "connection-2", null, PlayerSign.X);

            var document = record.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<TicTacToeGameRecord>(document);

            Assert.Equal("X--------", deserialized.Board);
            Assert.Equal("Bob", deserialized.Player2Name);
            Assert.Equal(PlayerKind.Human, deserialized.Player2Kind);
            Assert.Equal("connection-2", deserialized.Player2ConnectionId);
            Assert.Null(deserialized.Player2DifficultyLevel);
            Assert.Equal(PlayerSign.X, deserialized.CurrentTurnSign);
        }

        [Fact]
        public void StartedGameAgainstAComputerOpponent_RoundTripsThroughBson()
        {
            var record = CreateWaitingRecord();
            record.Start("X--------", "Computer", PlayerKind.Computer, null, DifficultyLevel.Hard, PlayerSign.X);

            var document = record.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<TicTacToeGameRecord>(document);

            Assert.Equal(PlayerKind.Computer, deserialized.Player2Kind);
            Assert.Null(deserialized.Player2ConnectionId);
            Assert.Equal(DifficultyLevel.Hard, deserialized.Player2DifficultyLevel);
        }

        [Fact]
        public void ApplyMove_ChangesRoundTripThroughBson()
        {
            var record = CreateWaitingRecord();
            record.Start("X--------", "Bob", PlayerKind.Human, "connection-2", null, PlayerSign.X);
            record.ApplyMove("XO-------", PlayerSign.O);

            var document = record.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<TicTacToeGameRecord>(document);

            Assert.Equal("XO-------", deserialized.Board);
            Assert.Equal(PlayerSign.O, deserialized.CurrentTurnSign);
        }
    }
}
