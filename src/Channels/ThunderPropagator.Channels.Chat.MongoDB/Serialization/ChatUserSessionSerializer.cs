using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Chat.Models.Sessions;

namespace ThunderPropagator.Channels.Chat.MongoDB.Serialization
{
    internal sealed class ChatUserSessionSerializer : ChatEntitySerializerBase<ChatUserSession>
    {
        private static readonly FieldInfo IdField = GetBackingField(nameof(ChatUserSession.Id));
        private static readonly FieldInfo ConnectionIdField = GetBackingField(nameof(ChatUserSession.ConnectionId));
        private static readonly FieldInfo UserIdField = GetBackingField(nameof(ChatUserSession.UserId));

        public override ChatUserSession Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var session = CreateInstance();

            reader.ReadStartDocument();
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                switch (reader.ReadName(Utf8NameDecoder.Instance))
                {
                    case "_id": IdField.SetValue(session, ReadGuid(reader)); break;
                    case "ConnectionId": ConnectionIdField.SetValue(session, ReadString(reader)); break;
                    case "UserId": UserIdField.SetValue(session, ReadGuid(reader)); break;
                    default: reader.SkipValue(); break;
                }
            }
            reader.ReadEndDocument();

            return session;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ChatUserSession value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            WriteGuid(writer, "_id", value.Id);
            WriteString(writer, "ConnectionId", value.ConnectionId);
            WriteGuid(writer, "UserId", value.UserId);
            writer.WriteEndDocument();
        }

        public override bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = memberName switch
            {
                nameof(ChatUserSession.Id) => GuidMemberInfo("_id"),
                nameof(ChatUserSession.ConnectionId) => StringMemberInfo("ConnectionId"),
                nameof(ChatUserSession.UserId) => GuidMemberInfo("UserId"),
                _ => null!
            };

            return serializationInfo is not null;
        }
    }
}
