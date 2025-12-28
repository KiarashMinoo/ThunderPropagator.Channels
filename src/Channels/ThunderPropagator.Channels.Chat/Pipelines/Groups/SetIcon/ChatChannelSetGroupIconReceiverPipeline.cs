using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using CaseConverter;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.SetIcon
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelSetGroupIconReceiverPipelineRequestDto))]
    [ReceivePipelineResponseSchema(typeof(ChatChannelSetGroupIconReceiverPipelineResponseDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSetGroupIconReceiverPipeline(
            ILoggerFactory loggerFactory,
            GroupService groupService,
            UserService userService,
            MessageService messageService)
        : AbstractReceivePipeline<ChatChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => $"{nameof(Groups)}/{nameof(SetIcon)}";

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
                if (context.Request.RouteTable["RequestType"].Equals(RequestKey))
                {
                    var setGroupIconRequest = context.Request.GetRequestContentFormData<ChatChannelSetGroupIconReceiverPipelineRequestDto>()!;

                    var chatChannel = (ChatChannel)channelInfo.Channel;
                    var userId = chatChannel.LoggedInUsers[context.WebSocketConnectionInfo.ConnectionId];
                    var user = await userService.GetByIdAsync(userId, cancellationToken) ?? throw new UserNotFoundException();
                    var group = await groupService.GetByIdAsync(setGroupIconRequest.GroupId, cancellationToken) ?? throw new GroupNotFoundException();

                    var newGroup = await groupService.SetGroupIconAsync(group.Id, setGroupIconRequest.Icon, cancellationToken);

                    //Send Added Message To User
                    chatChannel.EmitMessage(new ChatChannelFeederMessage(
                        await messageService.SendMessageAsync(user.Id, user.Id, $"you have changed group icon to {newGroup.GroupIcon}.", cancellationToken)
                    ));

                    //Send Add Message To Group
                    var messages = await messageService.SendMessageToGroupAsync(userId, group.Id, $"User {user.Name} changed group icon to {newGroup.GroupIcon}.", cancellationToken);
                    await Task.WhenAll(messages.Select(message =>
                    {
                        chatChannel.EmitMessage(new ChatChannelFeederMessage(message));
                        return Task.CompletedTask;
                    }));

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = new ChatChannelSetGroupIconReceiverPipelineResponseDto
                    {
                        Group = newGroup
                    };

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