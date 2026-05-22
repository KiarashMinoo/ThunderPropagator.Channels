using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels.Contexts;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Application.Pipelines.Receivers.Attributes;
using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Infrastructure.Channels;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.RemoveUser
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelRemoveUserToGroupReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelRemoveUserToGroupReceiverPipeline(
            ILoggerFactory loggerFactory,
            GroupService groupService,
            UserService userService,
            MessageService messageService)
        : AbstractReceivePipeline<ChatChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => $"{nameof(Groups)}/{nameof(RemoveUser)}";

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
                    var addUserRequest = context.Request.GetRequestContentFormData<ChatChannelRemoveUserToGroupReceiverPipelineRequestDto>()!;

                    var user = await userService.GetByIdAsync(addUserRequest.UserId, cancellationToken) ?? throw new UserNotFoundException();
                    var group = await groupService.GetByIdAsync(addUserRequest.GroupId, cancellationToken) ?? throw new GroupNotFoundException();

                    await groupService.RemoveUserFromGroupAsync(group.Id, user.Id, cancellationToken);

                    var chatChannel = (ChatChannel)channelInfo.Channel;
                    var senderId = chatChannel.LoggedInUsers[context.WebSocketConnectionInfo.ConnectionId];
                    var sender = (await userService.GetByIdAsync(senderId, cancellationToken))!;

                    //Send Added Message To User
                    chatChannel.EmitMessage(new ChatChannelFeederMessage(
                        await messageService.SendMessageAsync(senderId, user.Id, $"{sender.Name} has removed you from group {group.Name}.", cancellationToken)
                    ));

                    //Send Add Message To Group
                    var messages = await messageService.SendMessageToGroupAsync(senderId, group.Id, $"User {sender.Name} has removed user {user.Name} from group.", cancellationToken);
                    await Task.WhenAll(messages.Select(message =>
                    {
                        chatChannel.EmitMessage(new ChatChannelFeederMessage(message));
                        return Task.CompletedTask;
                    }));

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = "Removed";

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