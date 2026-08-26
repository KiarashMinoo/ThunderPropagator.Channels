using System.Net;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Channels.Chat.Channel;
using ThunderPropagator.Channels.Chat.Configuration;
using ThunderPropagator.Channels.Chat.Messages;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.MarkRead
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelMarkMessageReadReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(ChatChannelMarkMessageReadReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelMarkMessageReadReceiverPipeline(ILoggerFactory loggerFactory, MessageService messageService, ChatChannelConfiguration configuration)
        : AuthenticatedChatChannelReceiverPipeline(loggerFactory)
    {
        public override string RequestKey => $"{nameof(Messages)}/{nameof(MarkRead)}";

        protected override async Task InvokeAuthenticatedAsync(
            ChannelInfo channelInfo,
            ReceiveContext context,
            ChatChannel chatChannel,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var markReadRequest = context.Request.GetRequestContentFormData<ChatChannelMarkMessageReadReceiverPipelineRequestDto>()!;

            var result = await messageService.MarkMessagesReadAsync(currentUserId, markReadRequest.MessageIds, cancellationToken);

            if (configuration.ReadReceiptsEnabled)
            {
                foreach (var message in result.MarkedRead)
                    chatChannel.EmitMessage(new ChatChannelFeederMessage(message, isRead: true));
            }

            context.Response.ResponseCode = (int)HttpStatusCode.OK;
            context.Response.ResponseContent = new ChatChannelMarkMessageReadReceiverPipelineResponseDto
            {
                MarkedRead = result.MarkedRead.Select(message => message.Id).ToArray(),
                FailedMessageIds = result.FailedMessageIds
            };
        }
    }
}
