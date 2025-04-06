using Microsoft.EntityFrameworkCore;

namespace RapidStreamer.Channels.Chat.Models.Messages
{
    internal
#if !DEBUG
        sealed
#endif
        class MessageService(IChatContext chatContext)
    {
        public async Task<Message> SendMessageAsync(Guid senderId, Guid receiverId, string body, CancellationToken cancellationToken = default)
        {
            var message = Message.Create(senderId, receiverId, body);

            var entry = await chatContext.Messages.AddAsync(message, cancellationToken);

            await chatContext.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }

        public async Task<IReadOnlyCollection<Message>> SendMessageToGroupAsync(Guid senderId, Guid groupId, string body, CancellationToken cancellationToken = default)
        {
            List<Message> rtn = [];

            var group = await chatContext.Groups.SingleAsync(x => x.Id == groupId, cancellationToken);

            foreach (var groupUser in group.GroupUsers)
            {
                var message = Message.Create(senderId, groupUser.UserId, body);
                var entry = await chatContext.Messages.AddAsync(message, cancellationToken);
                rtn.Add(entry.Entity);
            }

            await chatContext.SaveChangesAsync(cancellationToken);

            return rtn.AsReadOnly();
        }
    }
}