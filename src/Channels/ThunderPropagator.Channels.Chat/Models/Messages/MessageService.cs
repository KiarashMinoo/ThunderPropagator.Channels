using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.Channels.Chat.Models.Messages
{
    internal
#if !DEBUG
        sealed
#endif
        class MessageService(IChatContext chatContext)
    {
        // Issue #117: shared page-size defaults/bounds every history caller validates against —
        // MaxPageSize keeps a single page cheap to serve regardless of how large a conversation's
        // history grows, per the AC's "queries execute in the provider and do not load the complete
        // history"; DefaultPageSize is what a caller gets when it doesn't specify one.
        public const int DefaultPageSize = 50;
        public const int MaxPageSize = 100;

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
                // Issue #117: previously called the 3-arg Message.Create overload, which never sets
                // GroupId — every group-fanned message was persisted (and emitted via
                // ChatChannelFeederMessage) indistinguishable from a direct message. GetGroupMessageHistoryAsync
                // depends on GroupId actually being set to filter a group's history at all.
                var message = Message.Create(senderId, groupUser.UserId, groupId, body);
                message = await chatContext.CreateAsync(message, cancellationToken);
                rtn.Add(message);
            }

            return rtn.AsReadOnly();
        }

        // Issue #117: currentUserId is always one side of the pair the provider filters on, so a
        // caller can never be handed a page from a conversation it isn't part of — there's no
        // separate authorization check to forget or bypass.
        public Task<MessageHistoryPage> GetDirectMessageHistoryAsync(Guid currentUserId, Guid otherUserId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            ValidatePaging(page, pageSize);

            return chatContext.GetDirectMessageHistoryAsync(currentUserId, otherUserId, page, pageSize, cancellationToken);
        }

        // Issue #117: unlike the direct case, group membership has to be checked explicitly —
        // requesting a group's history doesn't imply the caller belongs to it.
        public async Task<MessageHistoryPage> GetGroupMessageHistoryAsync(Guid currentUserId, Guid groupId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            ValidatePaging(page, pageSize);

            var group = await chatContext.GetAsync<Group, Guid>(groupId, cancellationToken) ?? throw new GroupNotFoundException();
            if (group.GroupUsers.All(groupUser => groupUser.UserId != currentUserId))
                throw new GroupAccessDeniedException();

            return await chatContext.GetGroupMessageHistoryAsync(groupId, page, pageSize, cancellationToken);
        }

        private static void ValidatePaging(int page, int pageSize)
        {
            if (page < 1)
                throw new InvalidMessageHistoryPageRequestException("Page must be 1 or greater.");

            if (pageSize is < 1 or > MaxPageSize)
                throw new InvalidMessageHistoryPageRequestException($"PageSize must be between 1 and {MaxPageSize}.");
        }
    }
}