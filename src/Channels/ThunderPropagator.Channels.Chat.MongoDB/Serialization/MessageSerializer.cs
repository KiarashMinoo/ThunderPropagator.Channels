using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.MongoDB.Context;

namespace ThunderPropagator.Channels.Chat.MongoDB.Serialization
{
    internal sealed class MessageSerializer : ChatEntitySerializerBase<Message>
    {
        private static readonly FieldInfo IdField = GetBackingField(nameof(Message.Id));
        private static readonly FieldInfo SenderIdField = GetBackingField(nameof(Message.SenderId));
        private static readonly FieldInfo ReceiverIdField = GetBackingField(nameof(Message.ReceiverId));
        private static readonly FieldInfo GroupIdField = GetBackingField(nameof(Message.GroupId));
        private static readonly FieldInfo CreatedField = GetBackingField(nameof(Message.Created));
        private static readonly FieldInfo BodyField = GetBackingField(nameof(Message.Body));
        private static readonly FieldInfo IsDeletedField = GetBackingField(nameof(Message.IsDeleted));
        private static readonly FieldInfo DeletedAtField = GetBackingField(nameof(Message.DeletedAt));
        private static readonly FieldInfo IsEditedField = GetBackingField(nameof(Message.IsEdited));
        private static readonly FieldInfo EditedAtField = GetBackingField(nameof(Message.EditedAt));
        private static readonly FieldInfo IsReadField = GetBackingField(nameof(Message.IsRead));
        private static readonly FieldInfo ReadAtField = GetBackingField(nameof(Message.ReadAt));

        // Message.Sender/.Receiver/.Group are intentionally not serialized — Sender is populated by
        // MongoDbChatContext after a read, for any consumer that displays a message's sender directly
        // (Message.Sender is public API). GetContactsAsync (#115) doesn't touch this navigation — it
        // projects SenderId/ReceiverId server-side instead. Receiver/Group are never populated this way.
        public override Message Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var message = CreateInstance();

            reader.ReadStartDocument();
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                switch (reader.ReadName(Utf8NameDecoder.Instance))
                {
                    case "_id": IdField.SetValue(message, ReadGuid(reader)); break;
                    case "SenderId": SenderIdField.SetValue(message, ReadGuid(reader)); break;
                    case "ReceiverId": ReceiverIdField.SetValue(message, ReadGuid(reader)); break;
                    case "GroupId": GroupIdField.SetValue(message, ReadNullableGuid(reader)); break;
                    case "Created": CreatedField.SetValue(message, ReadDateTimeOffset(reader)); break;
                    case "Body": BodyField.SetValue(message, ReadString(reader)); break;
                    case "IsDeleted": IsDeletedField.SetValue(message, ReadBool(reader)); break;
                    case "DeletedAt": DeletedAtField.SetValue(message, ReadNullableDateTimeOffset(reader)); break;
                    case "IsEdited": IsEditedField.SetValue(message, ReadBool(reader)); break;
                    case "EditedAt": EditedAtField.SetValue(message, ReadNullableDateTimeOffset(reader)); break;
                    case "IsRead": IsReadField.SetValue(message, ReadBool(reader)); break;
                    case "ReadAt": ReadAtField.SetValue(message, ReadNullableDateTimeOffset(reader)); break;
                    default: reader.SkipValue(); break;
                }
            }
            reader.ReadEndDocument();

            return message;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Message value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            WriteGuid(writer, "_id", value.Id);
            WriteGuid(writer, "SenderId", value.SenderId);
            WriteGuid(writer, "ReceiverId", value.ReceiverId);
            WriteNullableGuid(writer, "GroupId", value.GroupId);
            WriteDateTimeOffset(writer, "Created", value.Created);
            WriteString(writer, "Body", value.Body);
            WriteBool(writer, "IsDeleted", value.IsDeleted);
            WriteNullableDateTimeOffset(writer, "DeletedAt", value.DeletedAt);
            WriteBool(writer, "IsEdited", value.IsEdited);
            WriteNullableDateTimeOffset(writer, "EditedAt", value.EditedAt);
            WriteBool(writer, "IsRead", value.IsRead);
            WriteNullableDateTimeOffset(writer, "ReadAt", value.ReadAt);
            writer.WriteEndDocument();
        }

        public override bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = memberName switch
            {
                nameof(Message.Id) => GuidMemberInfo("_id"),
                nameof(Message.SenderId) => GuidMemberInfo("SenderId"),
                nameof(Message.ReceiverId) => GuidMemberInfo("ReceiverId"),
                nameof(Message.GroupId) => NullableGuidMemberInfo("GroupId"),
                nameof(Message.Body) => StringMemberInfo("Body"),
                nameof(Message.IsDeleted) => BoolMemberInfo("IsDeleted"),
                _ => null!
            };

            return serializationInfo is not null;
        }
    }
}
