using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;

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
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.messages.history";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total message-history requests received.");

        public override string RequestKey => $"{nameof(Messages)}/{nameof(History)}";
        protected override string ActivityName => TelemetryActivityName;
        protected override Counter<long>? RequestCounter => TelemetryRequestCounter;

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
