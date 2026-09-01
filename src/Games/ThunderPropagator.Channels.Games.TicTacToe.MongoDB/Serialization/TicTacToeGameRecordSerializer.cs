using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;
using ThunderPropagator.Channels.Games.TicTacToe.Models;

namespace ThunderPropagator.Channels.Games.TicTacToe.MongoDB.Serialization
{
    internal sealed class TicTacToeGameRecordSerializer : TicTacToeEntitySerializerBase<TicTacToeGameRecord>
    {
        private static readonly FieldInfo SessionIdField = GetBackingField(nameof(TicTacToeGameRecord.SessionId));
        private static readonly FieldInfo BoardField = GetBackingField(nameof(TicTacToeGameRecord.Board));
        private static readonly FieldInfo Player1NameField = GetBackingField(nameof(TicTacToeGameRecord.Player1Name));
        private static readonly FieldInfo Player1SignField = GetBackingField(nameof(TicTacToeGameRecord.Player1Sign));
        private static readonly FieldInfo Player1ConnectionIdField = GetBackingField(nameof(TicTacToeGameRecord.Player1ConnectionId));
        private static readonly FieldInfo Player2NameField = GetBackingField(nameof(TicTacToeGameRecord.Player2Name));
        private static readonly FieldInfo Player2KindField = GetBackingField(nameof(TicTacToeGameRecord.Player2Kind));
        private static readonly FieldInfo Player2ConnectionIdField = GetBackingField(nameof(TicTacToeGameRecord.Player2ConnectionId));
        private static readonly FieldInfo Player2DifficultyLevelField = GetBackingField(nameof(TicTacToeGameRecord.Player2DifficultyLevel));
        private static readonly FieldInfo CurrentTurnSignField = GetBackingField(nameof(TicTacToeGameRecord.CurrentTurnSign));

        public override TicTacToeGameRecord Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var game = CreateInstance();

            reader.ReadStartDocument();
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                switch (reader.ReadName(Utf8NameDecoder.Instance))
                {
                    case "_id": SessionIdField.SetValue(game, ReadString(reader)); break;
                    case "Board": BoardField.SetValue(game, ReadString(reader)); break;
                    case "Player1Name": Player1NameField.SetValue(game, ReadString(reader)); break;
                    case "Player1Sign": Player1SignField.SetValue(game, ReadEnum<PlayerSign>(reader)); break;
                    case "Player1ConnectionId": Player1ConnectionIdField.SetValue(game, ReadString(reader)); break;
                    case "Player2Name": Player2NameField.SetValue(game, ReadNullableString(reader)); break;
                    case "Player2Kind": Player2KindField.SetValue(game, ReadNullableEnum<PlayerKind>(reader)); break;
                    case "Player2ConnectionId": Player2ConnectionIdField.SetValue(game, ReadNullableString(reader)); break;
                    case "Player2DifficultyLevel": Player2DifficultyLevelField.SetValue(game, ReadNullableEnum<DifficultyLevel>(reader)); break;
                    case "CurrentTurnSign": CurrentTurnSignField.SetValue(game, ReadNullableEnum<PlayerSign>(reader)); break;
                    default: reader.SkipValue(); break;
                }
            }
            reader.ReadEndDocument();

            return game;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TicTacToeGameRecord value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            WriteString(writer, "_id", value.SessionId);
            WriteString(writer, "Board", value.Board);
            WriteString(writer, "Player1Name", value.Player1Name);
            WriteEnum(writer, "Player1Sign", value.Player1Sign);
            WriteString(writer, "Player1ConnectionId", value.Player1ConnectionId);
            WriteNullableString(writer, "Player2Name", value.Player2Name);
            WriteNullableEnum(writer, "Player2Kind", value.Player2Kind);
            WriteNullableString(writer, "Player2ConnectionId", value.Player2ConnectionId);
            WriteNullableEnum(writer, "Player2DifficultyLevel", value.Player2DifficultyLevel);
            WriteNullableEnum(writer, "CurrentTurnSign", value.CurrentTurnSign);
            writer.WriteEndDocument();
        }

        public override bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = memberName switch
            {
                nameof(TicTacToeGameRecord.SessionId) => StringMemberInfo("_id"),
                nameof(TicTacToeGameRecord.Player1Name) => StringMemberInfo("Player1Name"),
                nameof(TicTacToeGameRecord.Player1ConnectionId) => StringMemberInfo("Player1ConnectionId"),
                nameof(TicTacToeGameRecord.Player2Name) => StringMemberInfo("Player2Name"),
                nameof(TicTacToeGameRecord.Player2ConnectionId) => StringMemberInfo("Player2ConnectionId"),
                _ => null!
            };

            return serializationInfo is not null;
        }
    }
}
