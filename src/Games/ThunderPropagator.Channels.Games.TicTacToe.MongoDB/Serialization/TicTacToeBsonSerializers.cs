using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Games.TicTacToe.Models;

namespace ThunderPropagator.Channels.Games.TicTacToe.MongoDB.Serialization
{
    /// <summary>
    /// Registers the hand-written serializer for the TicTacToe domain entity — mirrors
    /// ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB's own RockPaperScissorsBsonSerializers.
    /// </summary>
    internal static class TicTacToeBsonSerializers
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

                BsonSerializer.RegisterSerializer(typeof(TicTacToeGameRecord), new TicTacToeGameRecordSerializer());

                _registered = true;
            }
        }
    }
}
