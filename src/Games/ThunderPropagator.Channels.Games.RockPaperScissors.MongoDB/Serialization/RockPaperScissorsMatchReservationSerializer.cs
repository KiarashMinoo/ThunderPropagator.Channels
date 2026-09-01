using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB.Serialization
{
    internal sealed class RockPaperScissorsMatchReservationSerializer : RockPaperScissorsEntitySerializerBase<RockPaperScissorsMatchReservation>
    {
        private static readonly FieldInfo ConnectionIdField = GetBackingField(nameof(RockPaperScissorsMatchReservation.ConnectionId));
        private static readonly FieldInfo ReservedAtField = GetBackingField(nameof(RockPaperScissorsMatchReservation.ReservedAt));

        public override RockPaperScissorsMatchReservation Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var reservation = CreateInstance();

            reader.ReadStartDocument();
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                switch (reader.ReadName(Utf8NameDecoder.Instance))
                {
                    case "_id": ConnectionIdField.SetValue(reservation, ReadString(reader)); break;
                    case "ReservedAt": ReservedAtField.SetValue(reservation, ReadDateTimeOffset(reader)); break;
                    default: reader.SkipValue(); break;
                }
            }
            reader.ReadEndDocument();

            return reservation;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, RockPaperScissorsMatchReservation value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            WriteString(writer, "_id", value.ConnectionId);
            WriteDateTimeOffset(writer, "ReservedAt", value.ReservedAt);
            writer.WriteEndDocument();
        }

        public override bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = memberName switch
            {
                nameof(RockPaperScissorsMatchReservation.ConnectionId) => StringMemberInfo("_id"),
                _ => null!
            };

            return serializationInfo is not null;
        }
    }
}
