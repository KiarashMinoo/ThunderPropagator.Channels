using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.Edit
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelEditMessageReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelEditMessageReceiverPipeline(ILoggerFactory loggerFactory, MessageService messageService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        public override string RequestKey => $"{nameof(Messages)}/{nameof(Edit)}";

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var editRequest = context.Request.GetRequestContentFormData<ChatChannelEditMessageReceiverPipelineRequestDto>()!;

            var message = await messageService.EditMessageAsync(currentUserId, editRequest.MessageId, editRequest.Body, cancellationToken);
            chatChannel.EmitMessage(new ChatChannelFeederMessage(message, isEdited: true));

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = "Edited";
        }
    }
}
