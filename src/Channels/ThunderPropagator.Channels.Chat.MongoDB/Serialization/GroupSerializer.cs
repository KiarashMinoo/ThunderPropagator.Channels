using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.Channels.Chat.MongoDB.Serialization
{
    internal sealed class GroupSerializer : ChatEntitySerializerBase<Group>
    {
        private static readonly FieldInfo IdField = GetBackingField(nameof(Group.Id));
        private static readonly FieldInfo NameField = GetBackingField(nameof(Group.Name));
        private static readonly FieldInfo GroupIconField = GetBackingField(nameof(Group.GroupIcon));

        // GroupUsers is intentionally not serialized here — GroupUser lives in its own collection
        // (see MongoDbChatContext), not embedded, so a real, globally-correct unique index on
        // (GroupId, UserId) can enforce "can't join the same group twice".
        public override Group Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var group = CreateInstance();

            reader.ReadStartDocument();
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                switch (reader.ReadName(Utf8NameDecoder.Instance))
                {
                    case "_id": IdField.SetValue(group, ReadGuid(reader)); break;
                    case "Name": NameField.SetValue(group, ReadString(reader)); break;
                    case "GroupIcon": GroupIconField.SetValue(group, ReadNullableString(reader)); break;
                    default: reader.SkipValue(); break;
                }
            }
            reader.ReadEndDocument();

            return group;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Group value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            WriteGuid(writer, "_id", value.Id);
            WriteString(writer, "Name", value.Name);
            WriteNullableString(writer, "GroupIcon", value.GroupIcon);
            writer.WriteEndDocument();
        }

        public override bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = memberName switch
            {
                nameof(Group.Id) => GuidMemberInfo("_id"),
                nameof(Group.Name) => StringMemberInfo("Name"),
                nameof(Group.GroupIcon) => StringMemberInfo("GroupIcon"),
                _ => null!
            };

            return serializationInfo is not null;
        }
    }
}
