using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.History
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelGetMessageHistoryReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(ChatChannelGetMessageHistoryReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGetMessageHistoryReceiverPipeline(ILoggerFactory loggerFactory, MessageService messageService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        public override string RequestKey => $"{nameof(Messages)}/{nameof(History)}";

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var request = context.Request.GetRequestContentFormData<ChatChannelGetMessageHistoryReceiverPipelineRequestDto>()!;
            request.ValidateTarget();

            var historyPage = request.ReceiverId is not null && request.ReceiverId != Guid.Empty
                ? await messageService.GetDirectMessageHistoryAsync(currentUserId, request.ReceiverId.Value, request.Page, request.PageSize, cancellationToken)
                : await messageService.GetGroupMessageHistoryAsync(currentUserId, request.GroupId!.Value, request.Page, request.PageSize, cancellationToken);

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = new ChatChannelGetMessageHistoryReceiverPipelineResponseDto
            {
                Messages = historyPage.Messages,
                TotalCount = historyPage.TotalCount,
                Page = historyPage.Page,
                PageSize = historyPage.PageSize
            };
        }
    }
}
