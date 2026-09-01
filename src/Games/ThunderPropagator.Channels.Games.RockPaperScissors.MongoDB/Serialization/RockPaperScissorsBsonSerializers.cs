using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB.Serialization
{
    /// <summary>
    /// Registers the hand-written serializers for the RockPaperScissors domain entities — mirrors
    /// ThunderPropagator.Channels.Chat.MongoDB's own ChatBsonSerializers.
    /// </summary>
    internal static class RockPaperScissorsBsonSerializers
    {
#if NET9_0_OR_GREATER
        private static readonly Lock RegistrationLock = new();
#else
        private static readonly object RegistrationLock = new();
#endif
        private static bool _registered;

        public static void EnsureRegistered()
        {
            lock (RegistrationLock)
            {
                if (_registered)
                    return;

                BsonSerializer.RegisterSerializer(typeof(RockPaperScissorsMatchReservation), new RockPaperScissorsMatchReservationSerializer());
                BsonSerializer.RegisterSerializer(typeof(RockPaperScissorsGameSessionRecord), new RockPaperScissorsGameSessionRecordSerializer());

                _registered = true;
            }
        }
    }
}
