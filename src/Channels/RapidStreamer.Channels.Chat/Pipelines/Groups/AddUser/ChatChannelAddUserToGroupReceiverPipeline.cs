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
using RapidStreamer.Channels.Chat.Models.Groups;
using RapidStreamer.Channels.Chat.Models.Messages;
using RapidStreamer.Channels.Chat.Models.Users;
using RapidStreamer.Infrastructure.Channels;

namespace RapidStreamer.Channels.Chat.Pipelines.Groups.AddUser
{
    [ReceivePipelineRequestSchema(typeof(ChatChannelAddUserToGroupReceiverPipelineRequestDto))]
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelAddUserToGroupReceiverPipeline(
            ILoggerFactory loggerFactory,
            GroupService groupService,
            UserService userService,
            MessageService messageService)
        : AbstractReceivePipeline<ChatChannel>(loggerFactory)
    {
        private Counter<long>? _counter;

        public override string RequestKey => $"{nameof(Groups)}/{nameof(AddUser)}";

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
                    var addUserRequest = context.Request.GetRequestContentFormData<ChatChannelAddUserToGroupReceiverPipelineRequestDto>()!;

                    var user = await userService.GetByIdAsync(addUserRequest.UserId, cancellationToken) ?? throw new UserNotFoundException();
                    var group = await groupService.GetByIdAsync(addUserRequest.GroupId, cancellationToken) ?? throw new GroupNotFoundException();

                    await groupService.AddUserToGroupAsync(group.Id, user.Id, cancellationToken);

                    var chatChannel = (ChatChannel)channelInfo.Channel;
                    var senderId = chatChannel.LoggedInUsers[context.WebSocketConnectionInfo.ConnectionId];
                    var sender = (await userService.GetByIdAsync(senderId, cancellationToken))!;

                    //Send Added Message To User
                    chatChannel.EmitMessage(new ChatChannelFeederMessage(
                        await messageService.SendMessageAsync(senderId, user.Id, $"{sender.Name} has added you to group {group.Name}.", cancellationToken)
                    ));

                    //Send Add Message To Group
                    var messages = await messageService.SendMessageToGroupAsync(senderId, group.Id, $"User {sender.Name} has added user {user.Name} to group.", cancellationToken);
                    await Task.WhenAll(messages.Select(message =>
                    {
                        chatChannel.EmitMessage(new ChatChannelFeederMessage(message));
                        return Task.CompletedTask;
                    }));

                    context.Response.ResponseCode = (int)HttpStatusCode.OK;
                    context.Response.ResponseContent = "Added";

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