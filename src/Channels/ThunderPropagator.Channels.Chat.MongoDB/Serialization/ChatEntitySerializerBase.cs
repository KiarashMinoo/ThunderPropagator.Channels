using System.Globalization;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ThunderPropagator.Channels.Chat.MongoDB.Serialization
{
    /// <summary>
    /// Base for the four Chat entity serializers (User, Group, GroupUser, Message).
    ///
    /// This exists instead of a <c>BsonClassMap</c>-based mapping (the usual, much less code way to
    /// do this) because <c>AutoMap()</c>'s behavior for these entities turned out to be both
    /// undocumented and inconsistent: it maps a get-only property with no matching constructor
    /// parameter only for the special "Id" member (via NamedIdMemberConvention) — every other
    /// get-only property (UserName, GroupId, UserId, SenderId, ReceiverId, Created, Body, ...) is
    /// silently dropped from the class map entirely, not deserialized at all, unless some
    /// constructor's parameters happen to match it by name — in which case it becomes a creator-map
    /// argument instead of a regular member, which breaks LINQ predicate translation
    /// (`GetMemberMap(name)` and the LINQ provider's own member lookup return nothing for it) and,
    /// empirically, didn't correctly deserialize its value either. Every one of these entities has
    /// several get-only properties, so there was no configuration of MapMember/MapField/AutoMap that
    /// covered all of them consistently. A hand-written serializer sidesteps all of it: construction
    /// always goes through the entity's own private parameterless constructor, and every field is
    /// read/written explicitly through the same compiler-generated backing field
    /// EntityFrameworkCore's configurations use field-based property access for (#110).
    ///
    /// Each serializer also implements <see cref="IBsonDocumentSerializer"/> so the MongoDB driver's
    /// LINQ provider can translate a predicate like <c>x.ReceiverId == id</c> into a native filter —
    /// without it, only serialization/deserialization would work, and every IChatContext query would
    /// silently fall back to pulling entire collections into memory to filter client-side.
    /// </summary>
    internal abstract class ChatEntitySerializerBase<TEntity> : SerializerBase<TEntity>, IBsonDocumentSerializer
        where TEntity : class
    {
        private static readonly ConstructorInfo ParameterlessConstructor =
            typeof(TEntity).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} has no parameterless constructor.");

        protected static FieldInfo GetBackingField(string propertyName)
            => typeof(TEntity).GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"{typeof(TEntity).Name} has no backing field for '{propertyName}'.");

        protected static TEntity CreateInstance() => (TEntity)ParameterlessConstructor.Invoke(null);

        protected static void WriteGuid(IBsonWriter writer, string name, Guid value)
        {
            writer.WriteName(name);
            writer.WriteBinaryData(new BsonBinaryData(value, GuidRepresentation.Standard));
        }

        protected static void WriteNullableGuid(IBsonWriter writer, string name, Guid? value)
        {
            writer.WriteName(name);
            if (value is null)
                writer.WriteNull();
            else
                writer.WriteBinaryData(new BsonBinaryData(value.Value, GuidRepresentation.Standard));
        }

        protected static void WriteString(IBsonWriter writer, string name, string value)
        {
            writer.WriteName(name);
            writer.WriteString(value);
        }

        protected static void WriteNullableString(IBsonWriter writer, string name, string? value)
        {
            writer.WriteName(name);
            if (value is null)
                writer.WriteNull();
            else
                writer.WriteString(value);
        }

        // Stored as an ISO 8601 string ("O") rather than a native BSON date: BSON's date type has no
        // offset component, and DateTimeOffset's whole point here (Message.Created) is to preserve it.
        protected static void WriteDateTimeOffset(IBsonWriter writer, string name, DateTimeOffset value)
        {
            writer.WriteName(name);
            writer.WriteString(value.ToString("O", CultureInfo.InvariantCulture));
        }

        // Stored as an ISO 8601 date string ("O") for the same reason — no native BSON date-only type.
        protected static void WriteNullableDateOnly(IBsonWriter writer, string name, DateOnly? value)
        {
            writer.WriteName(name);
            if (value is null)
                writer.WriteNull();
            else
                writer.WriteString(value.Value.ToString("O", CultureInfo.InvariantCulture));
        }

        // Issue #119: Message.DeletedAt — same ISO 8601 string representation as Created/WriteDateTimeOffset.
        protected static void WriteNullableDateTimeOffset(IBsonWriter writer, string name, DateTimeOffset? value)
        {
            writer.WriteName(name);
            if (value is null)
                writer.WriteNull();
            else
                writer.WriteString(value.Value.ToString("O", CultureInfo.InvariantCulture));
        }

        protected static void WriteBool(IBsonWriter writer, string name, bool value)
        {
            writer.WriteName(name);
            writer.WriteBoolean(value);
        }

        protected static Guid ReadGuid(IBsonReader reader) => reader.ReadBinaryData().AsGuid;

        protected static Guid? ReadNullableGuid(IBsonReader reader)
        {
            if (reader.CurrentBsonType == BsonType.Null)
            {
                reader.ReadNull();
                return null;
            }

            return reader.ReadBinaryData().AsGuid;
        }

        protected static string ReadString(IBsonReader reader) => reader.ReadString();

        protected static string? ReadNullableString(IBsonReader reader)
        {
            if (reader.CurrentBsonType == BsonType.Null)
            {
                reader.ReadNull();
                return null;
            }

            return reader.ReadString();
        }

        protected static DateTimeOffset ReadDateTimeOffset(IBsonReader reader)
            => DateTimeOffset.Parse(reader.ReadString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        protected static DateOnly? ReadNullableDateOnly(IBsonReader reader)
        {
            if (reader.CurrentBsonType == BsonType.Null)
            {
                reader.ReadNull();
                return null;
            }

            return DateOnly.Parse(reader.ReadString(), CultureInfo.InvariantCulture);
        }

        protected static DateTimeOffset? ReadNullableDateTimeOffset(IBsonReader reader)
        {
            if (reader.CurrentBsonType == BsonType.Null)
            {
                reader.ReadNull();
                return null;
            }

            return DateTimeOffset.Parse(reader.ReadString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        protected static bool ReadBool(IBsonReader reader) => reader.ReadBoolean();

        public abstract bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo);

        protected static BsonSerializationInfo GuidMemberInfo(string elementName)
            => new(elementName, new GuidSerializer(GuidRepresentation.Standard), typeof(Guid));

        protected static BsonSerializationInfo NullableGuidMemberInfo(string elementName)
            => new(elementName, new NullableSerializer<Guid>(new GuidSerializer(GuidRepresentation.Standard)), typeof(Guid?));

        protected static BsonSerializationInfo StringMemberInfo(string elementName)
            => new(elementName, new StringSerializer(), typeof(string));

        protected static BsonSerializationInfo BoolMemberInfo(string elementName)
            => new(elementName, new BooleanSerializer(), typeof(bool));
    }
}
