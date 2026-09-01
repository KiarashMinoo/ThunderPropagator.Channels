using System.Globalization;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB.Serialization
{
    /// <summary>
    /// Base for the RockPaperScissors entity serializers — mirrors
    /// ThunderPropagator.Channels.Chat.MongoDB's own ChatEntitySerializerBase (see its own comment for
    /// why these are hand-written instead of a BsonClassMap-based mapping), trimmed to only the
    /// primitive kinds RockPaperScissorsMatchReservation/RockPaperScissorsGameSessionRecord actually
    /// use (string, enum, DateTimeOffset — no Guid fields, since both entities use a natural string
    /// primary key).
    /// </summary>
    internal abstract class RockPaperScissorsEntitySerializerBase<TEntity> : SerializerBase<TEntity>, IBsonDocumentSerializer
        where TEntity : class
    {
        private static readonly ConstructorInfo ParameterlessConstructor =
            typeof(TEntity).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} has no parameterless constructor.");

        protected static FieldInfo GetBackingField(string propertyName)
            => typeof(TEntity).GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"{typeof(TEntity).Name} has no backing field for '{propertyName}'.");

        protected static TEntity CreateInstance() => (TEntity)ParameterlessConstructor.Invoke(null);

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

        protected static void WriteEnum<TEnum>(IBsonWriter writer, string name, TEnum value) where TEnum : struct, Enum
        {
            writer.WriteName(name);
            writer.WriteInt32(Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }

        // Stored as an ISO 8601 string ("O") rather than a native BSON date: BSON's date type has no
        // offset component, and this field's whole point is to preserve one.
        protected static void WriteDateTimeOffset(IBsonWriter writer, string name, DateTimeOffset value)
        {
            writer.WriteName(name);
            writer.WriteString(value.ToString("O", CultureInfo.InvariantCulture));
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

        protected static TEnum ReadEnum<TEnum>(IBsonReader reader) where TEnum : struct, Enum
            => (TEnum)(object)reader.ReadInt32();

        protected static DateTimeOffset ReadDateTimeOffset(IBsonReader reader)
            => DateTimeOffset.Parse(reader.ReadString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        public abstract bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo);

        protected static BsonSerializationInfo StringMemberInfo(string elementName)
            => new(elementName, new StringSerializer(), typeof(string));
    }
}
