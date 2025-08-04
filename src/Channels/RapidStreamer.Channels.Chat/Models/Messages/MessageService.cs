using RapidStreamer.Channels.Chat.Models.Groups;

namespace RapidStreamer.Channels.Chat.Models.Messages
{
    internal
#if !DEBUG
        sealed
#endif
        class MessageService(IChatContext chatContext)
    {
        public Task<Message> SendMessageAsync(Guid senderId, Guid receiverId, string body, CancellationToken cancellationToken = default)
        {
            var message = Message.Create(senderId, receiverId, body);

            return chatContext.CreateAsync(message, cancellationToken);
        }

        public async Task<IReadOnlyCollection<Message>> SendMessageToGroupAsync(Guid senderId, Guid groupId, string body, CancellationToken cancellationToken = default)
        {
            List<Message> rtn = [];

            var group = await chatContext.GetAsync<Group, Guid>(groupId, cancellationToken) ?? throw new GroupNotFoundException();

            foreach (var groupUser in group.GroupUsers)
            {
                var message = Message.Create(senderId, groupUser.UserId, body);
                message = await chatContext.CreateAsync(message, cancellationToken);
                rtn.Add(message);
            }

            await chatContext.UpdateAsync(group, cancellationToken);

            return rtn.AsReadOnly();
        }
    }
}