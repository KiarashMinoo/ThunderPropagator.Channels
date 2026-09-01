using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB.Serialization
{
    internal sealed class RockPaperScissorsGameSessionRecordSerializer : RockPaperScissorsEntitySerializerBase<RockPaperScissorsGameSessionRecord>
    {
        private static readonly FieldInfo SessionIdField = GetBackingField(nameof(RockPaperScissorsGameSessionRecord.SessionId));
        private static readonly FieldInfo FirstPlayerNameField = GetBackingField(nameof(RockPaperScissorsGameSessionRecord.FirstPlayerName));
        private static readonly FieldInfo FirstPlayerTypeField = GetBackingField(nameof(RockPaperScissorsGameSessionRecord.FirstPlayerType));
        private static readonly FieldInfo FirstPlayerMoveField = GetBackingField(nameof(RockPaperScissorsGameSessionRecord.FirstPlayerMove));
        private static readonly FieldInfo FirstPlayerConnectionIdField = GetBackingField(nameof(RockPaperScissorsGameSessionRecord.FirstPlayerConnectionId));
        private static readonly FieldInfo SecondPlayerNameField = GetBackingField(nameof(RockPaperScissorsGameSessionRecord.SecondPlayerName));
        private static readonly FieldInfo SecondPlayerTypeField = GetBackingField(nameof(RockPaperScissorsGameSessionRecord.SecondPlayerType));
        private static readonly FieldInfo SecondPlayerMoveField = GetBackingField(nameof(RockPaperScissorsGameSessionRecord.SecondPlayerMove));
        private static readonly FieldInfo SecondPlayerConnectionIdField = GetBackingField(nameof(RockPaperScissorsGameSessionRecord.SecondPlayerConnectionId));
        private static readonly FieldInfo PlayedAtField = GetBackingField(nameof(RockPaperScissorsGameSessionRecord.PlayedAt));

        public override RockPaperScissorsGameSessionRecord Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var session = CreateInstance();

            reader.ReadStartDocument();
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                switch (reader.ReadName(Utf8NameDecoder.Instance))
                {
                    case "_id": SessionIdField.SetValue(session, ReadString(reader)); break;
                    case "FirstPlayerName": FirstPlayerNameField.SetValue(session, ReadString(reader)); break;
                    case "FirstPlayerType": FirstPlayerTypeField.SetValue(session, ReadEnum<PlayerType>(reader)); break;
                    case "FirstPlayerMove": FirstPlayerMoveField.SetValue(session, ReadEnum<MoveKind>(reader)); break;
                    case "FirstPlayerConnectionId": FirstPlayerConnectionIdField.SetValue(session, ReadNullableString(reader)); break;
                    case "SecondPlayerName": SecondPlayerNameField.SetValue(session, ReadString(reader)); break;
                    case "SecondPlayerType": SecondPlayerTypeField.SetValue(session, ReadEnum<PlayerType>(reader)); break;
                    case "SecondPlayerMove": SecondPlayerMoveField.SetValue(session, ReadEnum<MoveKind>(reader)); break;
                    case "SecondPlayerConnectionId": SecondPlayerConnectionIdField.SetValue(session, ReadNullableString(reader)); break;
                    case "PlayedAt": PlayedAtField.SetValue(session, ReadDateTimeOffset(reader)); break;
                    default: reader.SkipValue(); break;
                }
            }
            reader.ReadEndDocument();

            return session;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, RockPaperScissorsGameSessionRecord value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            WriteString(writer, "_id", value.SessionId);
            WriteString(writer, "FirstPlayerName", value.FirstPlayerName);
            WriteEnum(writer, "FirstPlayerType", value.FirstPlayerType);
            WriteEnum(writer, "FirstPlayerMove", value.FirstPlayerMove);
            WriteNullableString(writer, "FirstPlayerConnectionId", value.FirstPlayerConnectionId);
            WriteString(writer, "SecondPlayerName", value.SecondPlayerName);
            WriteEnum(writer, "SecondPlayerType", value.SecondPlayerType);
            WriteEnum(writer, "SecondPlayerMove", value.SecondPlayerMove);
            WriteNullableString(writer, "SecondPlayerConnectionId", value.SecondPlayerConnectionId);
            WriteDateTimeOffset(writer, "PlayedAt", value.PlayedAt);
            writer.WriteEndDocument();
        }

        public override bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = memberName switch
            {
                nameof(RockPaperScissorsGameSessionRecord.SessionId) => StringMemberInfo("_id"),
                nameof(RockPaperScissorsGameSessionRecord.FirstPlayerName) => StringMemberInfo("FirstPlayerName"),
                nameof(RockPaperScissorsGameSessionRecord.FirstPlayerConnectionId) => StringMemberInfo("FirstPlayerConnectionId"),
                nameof(RockPaperScissorsGameSessionRecord.SecondPlayerName) => StringMemberInfo("SecondPlayerName"),
                nameof(RockPaperScissorsGameSessionRecord.SecondPlayerConnectionId) => StringMemberInfo("SecondPlayerConnectionId"),
                _ => null!
            };

            return serializationInfo is not null;
        }
    }
}
