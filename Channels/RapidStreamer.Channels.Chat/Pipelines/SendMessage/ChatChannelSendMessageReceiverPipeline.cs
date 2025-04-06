using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using CaseConverter;
using Microsoft.Extensions.Logging;
using RapidStreamer.Application.Channels.Contexts;
using RapidStreamer.Application.Pipelines.Receivers;
using RapidStreamer.Application.Pipelines.Receivers.Attributes;
using RapidStreamer.BuildingBlocks.Application;
using RapidStreamer.Channels.Chat.Models.Messages;
using RapidStreamer.Infrastructure.Channels;

namespace RapidStreamer.Channels.Chat.Pipelines.SendMessage
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelSendMessageReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSendMessageReceiverPipeline(ILoggerFactory loggerFactory, MessageService messageService) : AbstractReceivePipeline<ChatChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => nameof(SendMessage);

        public async Task Invoke(ChannelInfo channelInfo,
            ReceiveContext context,
            ReceivePipelineDelegate next,
            CancellationToken cancellationToken = default)
        {
            var activityName = $"{channelInfo.ChannelName}_{GetType().GetTypeInfo().Name}_{nameof(Invoke)}";
            _counter ??= Telemetry.CreateCounter<long>(activityName.ToSnakeCase());

            using var activity = Telemetry.StartActivity(activityName, ActivityKind.Consumer)?
                .SetTag(nameof(ChannelInfo.ChannelType), channelInfo.ChannelType)
                .SetTag(nameof(ChannelInfo.ChannelKey), channelInfo.ChannelKey)
                .SetTag(nameof(ChannelInfo.ChannelName), channelInfo.ChannelName);

            try
            {
                var createGroup = context.Request.RouteTable["RequestType"].Equals(RequestKey);
                if (createGroup)
                {
                    var sendMessageRequest = context.Request.GetRequestContentFormData<ChatChannelSendMessageReceiverPipelineRequestDto>()!;
                    if ((sendMessageRequest.ReceiverId is null || sendMessageRequest.ReceiverId == Guid.Empty) &&
                        (sendMessageRequest.GroupId is null || sendMessageRequest.GroupId == Guid.Empty))
                    {
                        throw new InvalidOperationException("One of the ReceiverId or GroupId are required.");
                    }

                    var chatChannel = (ChatChannel)channelInfo.Channel;
                    var senderId = chatChannel.LoggedInUsers[context.WebSocketConnectionInfo.ConnectionId];

                    if (sendMessageRequest.ReceiverId is not null && sendMessageRequest.ReceiverId != Guid.Empty)
                    {
                        var message = await messageService.SendMessageAsync(senderId, sendMessageRequest.ReceiverId.Value, sendMessageRequest.Body, cancellationToken);
                        chatChannel.EmitMessage(new ChatChannelFeederMessage(message));
                    }

                    if (sendMessageRequest.GroupId is not null && sendMessageRequest.GroupId != Guid.Empty)
                    {
                        var messages = await messageService.SendMessageToGroupAsync(senderId, sendMessageRequest.GroupId.Value, sendMessageRequest.Body, cancellationToken);
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