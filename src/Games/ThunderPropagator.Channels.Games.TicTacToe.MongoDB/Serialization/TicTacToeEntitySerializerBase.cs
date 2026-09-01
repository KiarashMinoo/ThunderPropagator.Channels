using System.Globalization;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ThunderPropagator.Channels.Games.TicTacToe.MongoDB.Serialization
{
    /// <summary>
    /// Base for the TicTacToe entity serializer — mirrors
    /// ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB's own
    /// RockPaperScissorsEntitySerializerBase (see that one's own comment, and
    /// ThunderPropagator.Channels.Chat.MongoDB's ChatEntitySerializerBase before it, for why these are
    /// hand-written instead of a BsonClassMap-based mapping), extended with nullable-enum helpers —
    /// TicTacToeGameRecord's Player2Kind/Player2DifficultyLevel/CurrentTurnSign are all nullable,
    /// unset until a second player actually joins.
    /// </summary>
    internal abstract class TicTacToeEntitySerializerBase<TEntity> : SerializerBase<TEntity>, IBsonDocumentSerializer
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

        protected static void WriteNullableEnum<TEnum>(IBsonWriter writer, string name, TEnum? value) where TEnum : struct, Enum
        {
            writer.WriteName(name);
            if (value is null)
                writer.WriteNull();
            else
                writer.WriteInt32(Convert.ToInt32(value.Value, CultureInfo.InvariantCulture));
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

        protected static TEnum? ReadNullableEnum<TEnum>(IBsonReader reader) where TEnum : struct, Enum
        {
            if (reader.CurrentBsonType == BsonType.Null)
            {
                reader.ReadNull();
                return null;
            }

            return (TEnum)(object)reader.ReadInt32();
        }

        public abstract bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo);

        protected static BsonSerializationInfo StringMemberInfo(string elementName)
            => new(elementName, new StringSerializer(), typeof(string));
    }
}
