using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Games.RockPaperScissors;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;
using ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB.Serialization;

namespace ThunderPropagator.UnitTests.Games.RockPaperScissors.MongoDB.Serialization
{
    /// <summary>
    /// Issue #288: RockPaperScissorsMatchReservation/RockPaperScissorsGameSessionRecord both have
    /// private constructors and get-only properties, so each has a hand-written serializer (mirrors
    /// ThunderPropagator.Channels.Chat.MongoDB's own ChatBsonSerializersTests — see that file's own
    /// comment for why). These round-trip real instances through ToBsonDocument()/BsonSerializer.Deserialize
    /// — pure in-memory BSON (de)serialization, no MongoDB server needed.
    /// </summary>
    public sealed class RockPaperScissorsBsonSerializersTests
    {
        public RockPaperScissorsBsonSerializersTests() => RockPaperScissorsBsonSerializers.EnsureRegistered();

        [Fact]
        public void MatchReservation_RoundTripsThroughBson()
        {
            var reservation = RockPaperScissorsMatchReservation.Create("connection-1");

            var document = reservation.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<RockPaperScissorsMatchReservation>(document);

            Assert.Equal(reservation.ConnectionId, deserialized.ConnectionId);
            Assert.Equal(reservation.ReservedAt, deserialized.ReservedAt);
        }

        [Fact]
        public void MatchReservation_ConnectionIdIsStoredUnderTheMongoIdField()
        {
            var reservation = RockPaperScissorsMatchReservation.Create("connection-1");

            var document = reservation.ToBsonDocument();

            Assert.True(document.Contains("_id"));
            Assert.Equal("connection-1", document["_id"].AsString);
        }

        [Fact]
        public void GameSessionRecord_RoundTripsThroughBson()
        {
            var session = RockPaperScissorsGameSessionRecord.Create(
                new Player("Alice", PlayerType.Human, MoveKind.Rock),
                new Player("Computer", PlayerType.Computer, MoveKind.Scissor));

            var document = session.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<RockPaperScissorsGameSessionRecord>(document);

            Assert.Equal(session.SessionId, deserialized.SessionId);
            Assert.Equal("Alice", deserialized.FirstPlayerName);
            Assert.Equal(PlayerType.Human, deserialized.FirstPlayerType);
            Assert.Equal(MoveKind.Rock, deserialized.FirstPlayerMove);
            Assert.Null(deserialized.FirstPlayerConnectionId);
            Assert.Equal("Computer", deserialized.SecondPlayerName);
            Assert.Equal(PlayerType.Computer, deserialized.SecondPlayerType);
            Assert.Equal(MoveKind.Scissor, deserialized.SecondPlayerMove);
            Assert.Equal(session.PlayedAt, deserialized.PlayedAt);
        }

        [Fact]
        public void GameSessionRecord_SessionIdIsStoredUnderTheMongoIdField()
        {
            var session = RockPaperScissorsGameSessionRecord.Create(
                new Player("Alice", PlayerType.Human, MoveKind.Rock),
                new Player("Computer", PlayerType.Computer, MoveKind.Scissor));

            var document = session.ToBsonDocument();

            Assert.True(document.Contains("_id"));
            Assert.Equal(session.SessionId, document["_id"].AsString);
        }
    }
}
