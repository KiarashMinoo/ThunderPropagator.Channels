using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.Delete
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelDeleteMessageReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelDeleteMessageReceiverPipeline(ILoggerFactory loggerFactory, MessageService messageService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        public override string RequestKey => $"{nameof(Messages)}/{nameof(Delete)}";

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
