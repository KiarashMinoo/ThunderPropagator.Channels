using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.MongoDB.Context;

namespace ThunderPropagator.Channels.Chat.MongoDB.Serialization
{
    internal sealed class GroupUserSerializer : ChatEntitySerializerBase<GroupUser>
    {
        private static readonly FieldInfo IdField = GetBackingField(nameof(GroupUser.Id));
        private static readonly FieldInfo GroupIdField = GetBackingField(nameof(GroupUser.GroupId));
        private static readonly FieldInfo UserIdField = GetBackingField(nameof(GroupUser.UserId));

        // GroupUser.Group/.User are intentionally not serialized — they're populated by
        // MongoDbChatContext after a read, not stored in the document.
        public override GroupUser Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var groupUser = CreateInstance();

            reader.ReadStartDocument();
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                switch (reader.ReadName(Utf8NameDecoder.Instance))
                {
                    case "_id": IdField.SetValue(groupUser, ReadGuid(reader)); break;
                    case "GroupId": GroupIdField.SetValue(groupUser, ReadGuid(reader)); break;
                    case "UserId": UserIdField.SetValue(groupUser, ReadGuid(reader)); break;
                    default: reader.SkipValue(); break;
                }
            }
            reader.ReadEndDocument();

            return groupUser;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, GroupUser value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            WriteGuid(writer, "_id", value.Id);
            WriteGuid(writer, "GroupId", value.GroupId);
            WriteGuid(writer, "UserId", value.UserId);
            writer.WriteEndDocument();
        }

        public override bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = memberName switch
            {
                nameof(GroupUser.Id) => GuidMemberInfo("_id"),
                nameof(GroupUser.GroupId) => GuidMemberInfo("GroupId"),
                nameof(GroupUser.UserId) => GuidMemberInfo("UserId"),
                _ => null!
            };

            return serializationInfo is not null;
        }
    }
}
