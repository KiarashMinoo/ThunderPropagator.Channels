using System.Collections.Concurrent;
using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Chat
{
    public
#if !DEBUG
        sealed
#endif
        class ChatChannel : AbstractChannel<ChatChannelMetadata>
    {
        internal ConcurrentDictionary<string, Guid> LoggedInUsers { get; } = new();

        public ChatChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        internal void EmitMessage(ChatChannelFeederMessage feederMessage) => base.EmitMessage(feederMessage);
    }
}