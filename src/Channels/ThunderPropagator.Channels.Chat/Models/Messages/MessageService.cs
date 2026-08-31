using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Configuration;
using ThunderPropagator.Channels.Chat.Messages;

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
            ValidateBodyLength(body);

            var message = Message.Create(senderId, receiverId, body);

            return chatContext.CreateAsync(message, cancellationToken);
        }

        // Issue #33: unlike GetGroupMessageHistoryAsync below (which already checks this), this fanned
        // a message out to every member of any group the caller named, with no check that senderId was
        // actually one of them — any authenticated user could send into a group they don't belong to
        // just by knowing its GroupId. Same membership check as GetGroupMessageHistoryAsync's own.
        public async Task<IReadOnlyCollection<Message>> SendMessageToGroupAsync(Guid senderId, Guid groupId, string body, CancellationToken cancellationToken = default)
        {
            ValidateBodyLength(body);

            List<Message> rtn = [];

            var group = await chatContext.GetAsync<Group, Guid>(groupId, cancellationToken) ?? throw new GroupNotFoundException();
            if (group.IsDeleted)
                throw new GroupNotFoundException();

            if (group.GroupUsers.All(groupUser => groupUser.UserId != senderId))
                throw new GroupAccessDeniedException();

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
        //
        // Issue #141: pageSize is now optional — omitting it (both REST callers pass null when the
        // query string doesn't specify one, and the WebSocket request DTO's PageSize is likewise
        // null when unset) falls back to configuration.MessageHistoryPageSize rather than a
        // hardcoded constant, so a host can tune the effective default without every caller needing
        // to know its value. MaxPageSize remains the hard per-request ceiling regardless.
        public Task<MessageHistoryPage> GetDirectMessageHistoryAsync(Guid currentUserId, Guid otherUserId, int page, int? pageSize = null, CancellationToken cancellationToken = default)
        {
            var effectivePageSize = pageSize ?? configuration.MessageHistoryPageSize;
            ValidatePaging(page, effectivePageSize);

            return chatContext.GetDirectMessageHistoryAsync(currentUserId, otherUserId, page, effectivePageSize, cancellationToken);
        }

        // Issue #117: unlike the direct case, group membership has to be checked explicitly —
        // requesting a group's history doesn't imply the caller belongs to it. Issue #141: pageSize
        // defaulting mirrors GetDirectMessageHistoryAsync above.
        public async Task<MessageHistoryPage> GetGroupMessageHistoryAsync(Guid currentUserId, Guid groupId, int page, int? pageSize = null, CancellationToken cancellationToken = default)
        {
            var effectivePageSize = pageSize ?? configuration.MessageHistoryPageSize;
            ValidatePaging(page, effectivePageSize);

            var group = await chatContext.GetAsync<Group, Guid>(groupId, cancellationToken) ?? throw new GroupNotFoundException();
            if (group.IsDeleted)
                throw new GroupNotFoundException();

            if (group.GroupUsers.All(groupUser => groupUser.UserId != currentUserId))
                throw new GroupAccessDeniedException();

            return await chatContext.GetGroupMessageHistoryAsync(groupId, page, effectivePageSize, cancellationToken);
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

            // Issue #141: "same rules as new messages" now includes MaxMessageLength too — a revised
            // body is held to the exact same limit SendMessageAsync enforces via ValidateBodyLength,
            // just surfaced as InvalidMessageEditException (this method's existing exception type)
            // rather than InvalidMessageSendException.
            if (body.Length > configuration.MaxMessageLength)
                throw new InvalidMessageEditException($"Body must not exceed {configuration.MaxMessageLength} characters (was {body.Length}).");

            message.Edit(body);
            await chatContext.UpdateAsync(message, cancellationToken);

            return message;
        }

        // Issue #125: every id is resolved independently and never throws for an individual failure
        // — an unknown id, a deleted message, and a message the caller isn't the recipient of are all
        // folded into the same FailedMessageIds bucket without a distinguishing reason, so a caller
        // can't use this to probe which ids exist versus who they belong to (the same "documented
        // response contract" #109 already established). Already-read messages short-circuit before
        // the write, so repeated (and concurrent) requests are idempotent and read state can never
        // regress back to unread — the same accepted concurrency trade-off as
        // DeleteMessageAsync/EditMessageAsync above (see #116).
        public async Task<MarkMessagesReadResult> MarkMessagesReadAsync(Guid currentUserId, IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken = default)
        {
            List<Message> markedRead = [];
            List<Guid> failedMessageIds = [];

            foreach (var messageId in messageIds)
            {
                var message = await chatContext.GetAsync<Message, Guid>(messageId, cancellationToken);
                if (message is null || message.IsDeleted || message.ReceiverId != currentUserId)
                {
                    failedMessageIds.Add(messageId);
                    continue;
                }

                if (!message.IsRead)
                {
                    message.MarkRead();
                    await chatContext.UpdateAsync(message, cancellationToken);
                }

                markedRead.Add(message);
            }

            return new MarkMessagesReadResult { MarkedRead = markedRead, FailedMessageIds = failedMessageIds };
        }

        private static void ValidatePaging(int page, int pageSize)
        {
            if (page < 1)
                throw new InvalidMessageHistoryPageRequestException("Page must be 1 or greater.");

            if (pageSize is < 1 or > MaxPageSize)
                throw new InvalidMessageHistoryPageRequestException($"PageSize must be between 1 and {MaxPageSize}.");
        }

        // Issue #141: shared by SendMessageAsync/SendMessageToGroupAsync — a single check point so
        // both the direct and group send paths (and, by extension, both the REST SendMessageAsync
        // endpoint and the WebSocket Messages/Send pipeline, which call these same methods) enforce
        // MaxMessageLength identically rather than each transport re-implementing it.
        //
        // Issue #38: also rejects a null/blank body. ChatChannelEndpoints.SendMessageAsync (REST) had
        // its own separate IsNullOrWhiteSpace(request.Body) pre-check, but the WebSocket Messages/Send
        // pipeline calls straight into SendMessageAsync/SendMessageToGroupAsync with no equivalent
        // guard — a transport-parity gap, since a WS caller could send a blank message a REST caller
        // never could. Enforcing it here, in the one method both send paths already share for
        // MaxMessageLength, closes that gap for both transports at once rather than duplicating a
        // second check into the pipeline. REST's own pre-check is now redundant but harmless (its
        // "Body must not be empty." response fires first; this method's own
        // InvalidMessageSendException, for any caller that reaches it, is already caught by that same
        // endpoint's catch block and mapped to an equivalent ValidationProblem) — left in place rather
        // than removed, to keep this change scoped to closing the WS gap.
        private void ValidateBodyLength(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidMessageSendException("Body cannot be empty.");

            if (body.Length > configuration.MaxMessageLength)
                throw new InvalidMessageSendException($"Body must not exceed {configuration.MaxMessageLength} characters (was {body.Length}).");
        }
    }
}