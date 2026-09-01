using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;
using ThunderPropagator.Channels.Chat.Messages;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.Delete
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelDeleteMessageReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelDeleteMessageReceiverPipeline(ILoggerFactory loggerFactory, MessageService messageService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        private const string TelemetryActivityName = "thunderpropagator.channels.chat.messages.delete";
        private static readonly Counter<long>? TelemetryRequestCounter =
            Telemetry.CreateCounter<long>(TelemetryActivityName, "{request}", "Total delete-message requests received.");

        public override string RequestKey => $"{nameof(Messages)}/{nameof(Delete)}";
        protected override string ActivityName => TelemetryActivityName;
        protected override Counter<long>? RequestCounter => TelemetryRequestCounter;

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var deleteRequest = context.Request.GetRequestContentFormData<ChatChannelDeleteMessageReceiverPipelineRequestDto>()!;

            var message = await messageService.DeleteMessageAsync(currentUserId, deleteRequest.MessageId, cancellationToken);
            chatChannel.EmitMessage(new ChatChannelFeederMessage(message, isDeleted: true));

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = "Deleted";
        }
    }
}
