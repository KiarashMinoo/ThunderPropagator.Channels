using System.Threading;
using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Chat.MongoDB.Serialization;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.MongoDB.Serialization
{
    /// <summary>
    /// Registers the hand-written serializers (see Serialization/ChatEntitySerializerBase.cs for why
    /// they're hand-written instead of a BsonClassMap-based mapping) for the Chat domain entities.
    /// </summary>
    internal static class ChatBsonSerializers
    {
#if NET9_0_OR_GREATER
        private static readonly Lock RegistrationLock = new();
#else
        private static readonly object RegistrationLock = new();
#endif
        private static bool _registered;

        public static void EnsureRegistered()
        {
            // A bare Interlocked flag would let a concurrent caller observe "already registered"
            // while the first caller is still mid-way through the RegisterSerializer calls below —
            // xUnit runs test classes in parallel by default, and each test class's constructor
            // calls this. A lock blocks every other caller until registration has fully completed.
            lock (RegistrationLock)
            {
                if (_registered)
                    return;

                BsonSerializer.RegisterSerializer(typeof(User), new UserSerializer());
                BsonSerializer.RegisterSerializer(typeof(Group), new GroupSerializer());
                BsonSerializer.RegisterSerializer(typeof(GroupUser), new GroupUserSerializer());
                BsonSerializer.RegisterSerializer(typeof(Message), new MessageSerializer());

                _registered = true;
            }
        }
    }
}
