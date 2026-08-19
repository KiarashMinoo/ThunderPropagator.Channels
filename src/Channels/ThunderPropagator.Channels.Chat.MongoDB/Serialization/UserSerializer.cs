using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.MongoDB.Serialization
{
    internal sealed class UserSerializer : ChatEntitySerializerBase<User>
    {
        private static readonly FieldInfo IdField = GetBackingField(nameof(User.Id));
        private static readonly FieldInfo UserNameField = GetBackingField(nameof(User.UserName));
        private static readonly FieldInfo PasswordHashField = GetBackingField(nameof(User.PasswordHash));
        private static readonly FieldInfo NameField = GetBackingField(nameof(User.Name));
        private static readonly FieldInfo AvatarField = GetBackingField(nameof(User.Avatar));
        private static readonly FieldInfo BioField = GetBackingField(nameof(User.Bio));
        private static readonly FieldInfo BirthDateField = GetBackingField(nameof(User.BirthDate));

        public override User Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var user = CreateInstance();

            reader.ReadStartDocument();
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                switch (reader.ReadName(Utf8NameDecoder.Instance))
                {
                    case "_id": IdField.SetValue(user, ReadGuid(reader)); break;
                    case "UserName": UserNameField.SetValue(user, ReadString(reader)); break;
                    case "PasswordHash": PasswordHashField.SetValue(user, ReadString(reader)); break;
                    case "Name": NameField.SetValue(user, ReadString(reader)); break;
                    case "Avatar": AvatarField.SetValue(user, ReadNullableString(reader)); break;
                    case "Bio": BioField.SetValue(user, ReadNullableString(reader)); break;
                    case "BirthDate": BirthDateField.SetValue(user, ReadNullableDateOnly(reader)); break;
                    default: reader.SkipValue(); break;
                }
            }
            reader.ReadEndDocument();

            return user;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, User value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            WriteGuid(writer, "_id", value.Id);
            WriteString(writer, "UserName", value.UserName);
            WriteString(writer, "PasswordHash", value.PasswordHash);
            WriteString(writer, "Name", value.Name);
            WriteNullableString(writer, "Avatar", value.Avatar);
            WriteNullableString(writer, "Bio", value.Bio);
            WriteNullableDateOnly(writer, "BirthDate", value.BirthDate);
            writer.WriteEndDocument();
        }

        public override bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = memberName switch
            {
                nameof(User.Id) => GuidMemberInfo("_id"),
                nameof(User.UserName) => StringMemberInfo("UserName"),
                nameof(User.PasswordHash) => StringMemberInfo("PasswordHash"),
                nameof(User.Name) => StringMemberInfo("Name"),
                nameof(User.Avatar) => StringMemberInfo("Avatar"),
                nameof(User.Bio) => StringMemberInfo("Bio"),
                _ => null!
            };

            return serializationInfo is not null;
        }
    }
}
