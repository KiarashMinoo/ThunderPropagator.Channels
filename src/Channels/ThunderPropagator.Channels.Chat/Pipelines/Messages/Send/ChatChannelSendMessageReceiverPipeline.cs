using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.Send
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelSendMessageReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSendMessageReceiverPipeline(ILoggerFactory loggerFactory, MessageService messageService) : AbstractReceivePipeline<ChatChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => $"{nameof(Messages)}/{nameof(Send)}";

        public async Task Invoke(ChannelInfo channelInfo,
            ReceiveContext context,
            ReceivePipelineDelegate next,
            CancellationToken cancellationToken = default)
        {
            var activityName = $"{channelInfo.ChannelName}_{GetType().GetTypeInfo().Name}_{nameof(Invoke)}";
            _counter ??= Telemetry.CreateCounter<long>($"thunderpropagator.{activityName.ToLowerInvariant().Replace('_', '.')}");

            using var activity = Telemetry.StartActivity(activityName, ActivityKind.Consumer)?
                .SetTag(nameof(ChannelInfo.ChannelType), channelInfo.ChannelType)
                .SetTag(nameof(ChannelInfo.ChannelKey), channelInfo.ChannelKey)
                .SetTag(nameof(ChannelInfo.ChannelName), channelInfo.ChannelName);

            try
            {
                if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
                {
                    var sendMessageRequest = context.Request.GetRequestContentFormData<ChatChannelSendMessageReceiverPipelineRequestDto>()!;
                    sendMessageRequest.ValidateTarget();

                    var chatChannel = (ChatChannel)channelInfo.Channel;
                    var senderId = chatChannel.LoggedInUsers[context.WebSocketConnectionInfo.ConnectionId];

                    if (sendMessageRequest.ReceiverId is not null && sendMessageRequest.ReceiverId != Guid.Empty)
                    {
                        var message = await messageService.SendMessageAsync(senderId, sendMessageRequest.ReceiverId.Value, sendMessageRequest.Body, cancellationToken);
                        chatChannel.EmitMessage(new ChatChannelFeederMessage(message));
                    }
                    else
                    {
                        var messages = await messageService.SendMessageToGroupAsync(senderId, sendMessageRequest.GroupId!.Value, sendMessageRequest.Body, cancellationToken);
                        await Task.WhenAll(messages.Select(message =>
                        {
                            chatChannel.EmitMessage(new ChatChannelFeederMessage(message));
                            return Task.CompletedTask;
                        }));
                    }

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = "Sent";

                    _counter?.Add(1, new KeyValuePair<string, object?>(nameof(channelInfo.ChannelName), channelInfo.ChannelName));
                }
                else
                {
                    await next(context, cancellationToken);
                }
            }
            finally
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
        }
    }
}