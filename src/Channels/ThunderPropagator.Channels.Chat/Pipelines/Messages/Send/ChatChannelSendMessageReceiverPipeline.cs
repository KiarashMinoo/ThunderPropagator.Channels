using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;
using ThunderPropagator.Channels.Chat.Messages;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.Send
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelSendMessageReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSendMessageReceiverPipeline(ILoggerFactory loggerFactory, MessageService messageService) : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        public override string RequestKey => $"{nameof(Messages)}/{nameof(Send)}";

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var sendMessageRequest = context.Request.GetRequestContentFormData<ChatChannelSendMessageReceiverPipelineRequestDto>()!;
            sendMessageRequest.ValidateTarget();

            if (sendMessageRequest.ReceiverId is not null && sendMessageRequest.ReceiverId != Guid.Empty)
            {
                var message = await messageService.SendMessageAsync(currentUserId, sendMessageRequest.ReceiverId.Value, sendMessageRequest.Body, cancellationToken);
                chatChannel.EmitMessage(new ChatChannelFeederMessage(message));
            }
            else
            {
                var messages = await messageService.SendMessageToGroupAsync(currentUserId, sendMessageRequest.GroupId!.Value, sendMessageRequest.Body, cancellationToken);
                await Task.WhenAll(messages.Select(message =>
                {
                    chatChannel.EmitMessage(new ChatChannelFeederMessage(message));
                    return Task.CompletedTask;
                }));
            }

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = "Sent";
        }
    }
}