using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.Channels.Chat.Models.Messages
{
    internal
#if !DEBUG
        sealed
#endif
        class MessageService(IChatContext chatContext, ChatChannelConfiguration configuration)
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
            if (group.IsDeleted)
                throw new GroupNotFoundException();

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
            if (group.IsDeleted)
                throw new GroupNotFoundException();

            if (group.GroupUsers.All(groupUser => groupUser.UserId != currentUserId))
                throw new GroupAccessDeniedException();

            return await chatContext.GetGroupMessageHistoryAsync(groupId, page, pageSize, cancellationToken);
        }

        // Issue #119: unknown ids and the wrong sender each map to their own exception (404 vs 403)
        // so the pipeline can turn them into distinct, safe responses rather than one ambiguous
        // failure. An already-deleted message short-circuits before the write, so a repeated delete
        // request by the rightful sender is idempotent: no error, no redundant persistence, the same
        // successful result as the first call. Under a genuine race — two concurrent calls both
        // reading the not-yet-deleted row before either write commits — both would still pass this
        // check and both call UpdateAsync/emit a notification; that's an accepted trade-off given
        // this domain has no concurrency-token infrastructure anywhere else (see #116), and a
        // redundant delete notification is harmless for a client that already treats deletion as
        // idempotent. Issue #124: a group's admin (its creator) may also delete any message sent to
        // that group, not just their own — moderation within their own group only; a direct message
        // still only its sender can ever delete.
        public async Task<Message> DeleteMessageAsync(Guid currentUserId, Guid messageId, CancellationToken cancellationToken = default)
        {
            var message = await chatContext.GetAsync<Message, Guid>(messageId, cancellationToken) ?? throw new MessageNotFoundException();

            if (message.SenderId != currentUserId && !await IsGroupAdminAsync(message.GroupId, currentUserId, cancellationToken))
                throw new MessageDeleteForbiddenException();

            if (!message.IsDeleted)
            {
                message.MarkDeleted();
                await chatContext.UpdateAsync(message, cancellationToken);
            }

            return message;
        }

        private async Task<bool> IsGroupAdminAsync(Guid? groupId, Guid currentUserId, CancellationToken cancellationToken)
        {
            if (groupId is null)
                return false;

            var group = await chatContext.GetAsync<Group, Guid>(groupId.Value, cancellationToken);
            return group is not null && group.CreatedByUserId == currentUserId;
        }

        // Issue #120: a soft-deleted message is treated as not found rather than editable-but-blank
        // — #119 excludes deleted messages from history entirely, and letting anyone "edit" one back
        // into existence would undermine that. Ownership is checked before the window, so a
        // non-sender always gets Forbidden regardless of how stale the message is, never a timing
        // hint. "Same rules as new messages" (the AC) is, today, just presence: SendMessageAsync
        // itself enforces no content rules beyond Body being non-null (see Message's constructor) —
        // there is nothing stricter to reuse, so this only rejects a null/blank body, matching that
        // baseline rather than inventing new restrictions send doesn't have. Like DeleteMessageAsync,
        // a genuine concurrent race between two edits (or an edit racing a delete) is accepted:
        // whichever write commits last wins, consistent with this domain having no concurrency-token
        // infrastructure anywhere else (#116).
        public async Task<Message> EditMessageAsync(Guid currentUserId, Guid messageId, string body, CancellationToken cancellationToken = default)
        {
            var message = await chatContext.GetAsync<Message, Guid>(messageId, cancellationToken);
            if (message is null || message.IsDeleted)
                throw new MessageNotFoundException();

            if (message.SenderId != currentUserId)
                throw new MessageEditForbiddenException();

            if (DateTimeOffset.UtcNow - message.Created > configuration.MessageEditWindow)
                throw new MessageEditWindowExpiredException();

            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidMessageEditException("Body cannot be empty.");

            message.Edit(body);
            await chatContext.UpdateAsync(message, cancellationToken);

            return message;
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